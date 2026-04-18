using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BorsaBot.Models;

namespace BorsaBot.Core
{
    public class BotEngine : IDisposable
    {
        private readonly ApiClient _api;
        private readonly MemoryReader _memory;
        private readonly PacketManager _packet;
        private readonly BotConfig _config;
        private CancellationTokenSource? _cts;
        private readonly HashSet<int> _islenmisPazarlar = new();
        private readonly object _setKilit = new();

        public bool Calisiyor { get; private set; }
        public int ToplamTarama { get; private set; }
        public int ToplamAlis { get; private set; }
        public int ToplamKar { get; private set; }

        public event Action<string>? LogEvent;
        public event Action<MarketItem, TradeSignal>? SinyalEvent;

        public BotEngine(BotConfig config)
        {
            _config = config;
            _api = new ApiClient(config.ApiUrl);
            _memory = new MemoryReader();
            _packet = new PacketManager("127.0.0.1", 13000, config.PaketGonderimGecikmesiMs);
        }

        public async Task<bool> Baslat()
        {
            if (Calisiyor) return false;

            bool sunucuAktif = await _api.SunucuAktifMi();
            if (!sunucuAktif)
            {
                LogYaz("[HATA] Python motoru yanit vermiyor.");
                return false;
            }

            bool baglandi = _memory.Baglani(_config.OyunProsesAdi);
            if (!baglandi)
            {
                LogYaz($"[HATA] Proses bulunamadi: {_config.OyunProsesAdi}");
                return false;
            }

            _packet.Baglan();

            Calisiyor = true;
            _cts = new CancellationTokenSource();

            LogYaz("[OK] Bot aktif.");
            LogYaz($"[OK] Hiz: {_config.TaramaHiziMs}ms | MinKar: {_config.MinKarMarji:N0}");
            LogYaz($"[OK] AntiDetect: {(_config.AntiDetect ? "Acik" : "Kapali")}");

            _ = Task.Run(() => TaramaDongusu(_cts.Token));
            _ = Task.Run(() => EnvanterKontrolDongusu(_cts.Token));
            return true;
        }

        public void Durdur()
        {
            _cts?.Cancel();
            Calisiyor = false;
            LogYaz("[STOP] Bot durduruldu.");
            LogYaz($"[OZET] Tarama:{ToplamTarama} | Alis:{ToplamAlis} | Kar:{ToplamKar:N0}");
        }

        private async Task TaramaDongusu(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!_memory.BagliMi())
                    {
                        LogYaz("[UYARI] Oyun baglantisi koptu.");
                        Durdur();
                        break;
                    }

                    var pazarListesi = _memory.PazarListesiOku();
                    ToplamTarama++;

                    if (pazarListesi.Count > 0)
                        LogYaz($"[TARAMA #{ToplamTarama}] {pazarListesi.Count} item bulundu.");

                    var gorevler = new List<Task>();
                    foreach (var item in pazarListesi)
                    {
                        if (token.IsCancellationRequested) break;
                        bool zatenIslendi;
                        lock (_setKilit)
                            zatenIslendi = _islenmisPazarlar.Contains(item.PazarId);
                        if (!zatenIslendi)
                            gorevler.Add(ItemAnalizEt(item, token));
                    }

                    await Task.WhenAll(gorevler);

                    lock (_setKilit)
                    {
                        if (_islenmisPazarlar.Count > 5000)
                            _islenmisPazarlar.Clear();
                    }

                    var paket = _packet.PazarYenilePaketi();
                    await _packet.PaketGonderAsync(paket);

                    int bekleme = _config.TaramaHiziMs;
                    if (_config.AntiDetect)
                        bekleme += Random.Shared.Next(-100, 250);

                    await Task.Delay(Math.Max(300, bekleme), token);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    LogYaz($"[HATA] {ex.Message}");
                    await Task.Delay(2000, token);
                }
            }
        }

        private async Task EnvanterKontrolDongusu(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(30000, token);
                    LogYaz("[ENV] Envanter kontrol ediliyor...");
                }
                catch (TaskCanceledException) { }
            }
        }

        private async Task ItemAnalizEt(MarketItem item, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            var sinyal = await _api.AnalizEt(item);
            if (sinyal == null) return;

            SinyalEvent?.Invoke(item, sinyal);

            if (sinyal.Aksiyon == Aksiyon.AL && sinyal.KarMarji >= _config.MinKarMarji)
            {
                LogYaz($"[AL] {item.ItemAdi}");
                LogYaz($"     Fiyat:{item.Fiyat:N0} | Kar:{sinyal.KarMarji:N0} | Guven:{sinyal.GuvenSkoru:P0}");
                LogYaz($"     {sinyal.Sebep}");

                await SatinAl(item, sinyal);
            }
        }

        private async Task SatinAl(MarketItem item, TradeSignal sinyal)
        {
            if (_config.AntiDetect)
                await Task.Delay(Random.Shared.Next(60, 220));

            var paket = _packet.SatinAlPaketi(sinyal.PazarId, item.Miktar);
            var sonuc = await _packet.PaketGonderAsync(paket);

            if (sonuc.Basarili)
            {
                lock (_setKilit)
                    _islenmisPazarlar.Add(sinyal.PazarId);

                ToplamAlis++;
                ToplamKar += sinyal.KarMarji;
                LogYaz($"[OK] Satin alindi -> PazarID:{sinyal.PazarId}");

                if (_config.OtomatikSatis)
                {
                    if (_config.AntiDetect)
                        await Task.Delay(Random.Shared.Next(800, 2000));

                    var pazarKurPaketi = _packet.PazarKurPaketi(
                        sinyal.PazarId,
                        sinyal.HedefSatisFiyati,
                        item.Miktar
                    );
                    await _packet.PaketGonderAsync(pazarKurPaketi);
                    LogYaz($"[PAZAR] {item.ItemAdi} -> {sinyal.HedefSatisFiyati:N0} fiyata kuruldu.");
                }
            }
            else
            {
                LogYaz($"[HATA] Satin alma basarisiz: {sonuc.Mesaj}");
            }
        }

        private void LogYaz(string mesaj)
        {
            LogEvent?.Invoke($"[{DateTime.Now:HH:mm:ss}] {mesaj}");
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _api.Dispose();
            _memory.Dispose();
            _packet.Dispose();
        }
    }
}