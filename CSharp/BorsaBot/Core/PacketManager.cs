using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BorsaBot.Core
{
    public enum PaketTip : byte
    {
        SatinAl = 0xC8,
        PazarKur = 0xC9,
        PazarKapat = 0xCA,
        PazarListeIste = 0xCB,
        PazarYenile = 0xCC
    }

    public class PaketSonuc
    {
        public bool Basarili { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public byte[] HamVeri { get; set; } = Array.Empty<byte>();
    }

    public class PacketManager : IDisposable
    {
        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern IntPtr socket(int af, int type, int protocol);

        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern int send(IntPtr s, byte[] buf, int len, int flags);

        private Socket? _socket;
        private readonly string _ip;
        private readonly int _port;
        private readonly int _gonderimGecikmesi;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ConcurrentQueue<byte[]> _paketKuyruğu = new();
        private CancellationTokenSource? _cts;

        private readonly List<PaketLog> _paketGecmisi = new();
        private readonly object _logKilit = new();

        public bool Baglanildi => _socket?.Connected ?? false;
        public int GonderimSayisi { get; private set; }

        public PacketManager(string ip, int port, int gonderimGecikmesi = 120)
        {
            _ip = ip;
            _port = port;
            _gonderimGecikmesi = gonderimGecikmesi;
        }

        public bool Baglan()
        {
            try
            {
                _socket?.Dispose();
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true,
                    SendTimeout = 3000,
                    ReceiveTimeout = 3000
                };
                _socket.Connect(_ip, _port);
                _cts = new CancellationTokenSource();
                _ = Task.Run(KuyrukIsleyici);
                return true;
            }
            catch { return false; }
        }

        private async Task KuyrukIsleyici()
        {
            while (_cts != null && !_cts.Token.IsCancellationRequested)
            {
                if (_paketKuyruğu.TryDequeue(out var paket))
                {
                    await GonderInternal(paket);
                    await Task.Delay(_gonderimGecikmesi);
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }

        public void PaketKuyruğaEkle(byte[] paket)
        {
            _paketKuyruğu.Enqueue(paket);
        }

        public async Task<PaketSonuc> PaketGonderAsync(byte[] paket)
        {
            return await GonderInternal(paket);
        }

        private async Task<PaketSonuc> GonderInternal(byte[] paket)
        {
            if (_socket == null || !_socket.Connected)
                return new PaketSonuc { Basarili = false, Mesaj = "Baglanti yok" };

            await _semaphore.WaitAsync();
            try
            {
                byte[] sifrelenmis = XorSifrele(paket);
                _socket.Send(sifrelenmis);
                GonderimSayisi++;

                lock (_logKilit)
                {
                    _paketGecmisi.Add(new PaketLog
                    {
                        Zaman = DateTime.Now,
                        Tip = paket.Length > 0 ? (PaketTip)paket[0] : PaketTip.SatinAl,
                        Boyut = paket.Length
                    });
                    if (_paketGecmisi.Count > 1000)
                        _paketGecmisi.RemoveAt(0);
                }

                return new PaketSonuc
                {
                    Basarili = true,
                    Mesaj = $"Gonderildi ({paket.Length} byte)",
                    HamVeri = paket
                };
            }
            catch (Exception ex)
            {
                return new PaketSonuc { Basarili = false, Mesaj = ex.Message };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private byte[] XorSifrele(byte[] veri)
        {
            byte[] sonuc = new byte[veri.Length];
            byte anahtar = 0x42;
            for (int i = 0; i < veri.Length; i++)
                sonuc[i] = (byte)(veri[i] ^ anahtar);
            return sonuc;
        }

        private ushort CrcHesapla(byte[] veri)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in veri)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                    crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
            return crc;
        }

        public byte[] SatinAlPaketi(int pazarId, int miktar = 1)
        {
            byte[] paket = new byte[12];
            paket[0] = (byte)PaketTip.SatinAl;
            paket[1] = 0x0C;
            paket[2] = 0x00;
            BitConverter.GetBytes(pazarId).CopyTo(paket, 3);
            BitConverter.GetBytes(miktar).CopyTo(paket, 7);
            var crc = CrcHesapla(paket[..10]);
            BitConverter.GetBytes(crc).CopyTo(paket, 10);
            return paket;
        }

        public byte[] PazarKurPaketi(int itemId, int fiyat, int miktar = 1, string not = "")
        {
            byte[] notBytes = Encoding.UTF8.GetBytes(not.Length > 20 ? not[..20] : not);
            int toplamBoyut = 20 + notBytes.Length;
            byte[] paket = new byte[toplamBoyut];
            paket[0] = (byte)PaketTip.PazarKur;
            paket[1] = (byte)(toplamBoyut & 0xFF);
            paket[2] = (byte)(toplamBoyut >> 8);
            BitConverter.GetBytes(itemId).CopyTo(paket, 3);
            BitConverter.GetBytes(fiyat).CopyTo(paket, 7);
            BitConverter.GetBytes(miktar).CopyTo(paket, 11);
            paket[15] = 0x00;
            notBytes.CopyTo(paket, 16);
            var crc = CrcHesapla(paket[..(toplamBoyut - 2)]);
            BitConverter.GetBytes(crc).CopyTo(paket, toplamBoyut - 2);
            return paket;
        }

        public byte[] PazarKapatPaketi(int pazarId)
        {
            byte[] paket = new byte[9];
            paket[0] = (byte)PaketTip.PazarKapat;
            paket[1] = 0x09;
            paket[2] = 0x00;
            BitConverter.GetBytes(pazarId).CopyTo(paket, 3);
            var crc = CrcHesapla(paket[..7]);
            BitConverter.GetBytes(crc).CopyTo(paket, 7);
            return paket;
        }

        public byte[] PazarListeIstePaketi(int haritaId = 1)
        {
            byte[] paket = new byte[9];
            paket[0] = (byte)PaketTip.PazarListeIste;
            paket[1] = 0x09;
            paket[2] = 0x00;
            BitConverter.GetBytes(haritaId).CopyTo(paket, 3);
            var crc = CrcHesapla(paket[..7]);
            BitConverter.GetBytes(crc).CopyTo(paket, 7);
            return paket;
        }

        public byte[] PazarYenilePaketi()
        {
            byte[] paket = new byte[5];
            paket[0] = (byte)PaketTip.PazarYenile;
            paket[1] = 0x05;
            paket[2] = 0x00;
            var crc = CrcHesapla(paket[..3]);
            BitConverter.GetBytes(crc).CopyTo(paket, 3);
            return paket;
        }

        public List<PaketLog> GecmisiAl()
        {
            lock (_logKilit)
                return new List<PaketLog>(_paketGecmisi);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _semaphore.Dispose();
            _socket?.Close();
            _socket?.Dispose();
            _cts?.Dispose();
        }
    }

    public class PaketLog
    {
        public DateTime Zaman { get; set; }
        public PaketTip Tip { get; set; }
        public int Boyut { get; set; }
    }
}