using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BorsaBot.Core
{
    public class MemoryReader : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(
            IntPtr hProcess, IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public uint dwOemId;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_WRITECOPY = 0x08;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;

        private Process? _process;
        private IntPtr _handle = IntPtr.Zero;
        private IntPtr _baseAdres = IntPtr.Zero;

        public static class Adresler
        {
            public const long PAZAR_LISTE_POINTER = 0x00E47A90;
            public const int PAZAR_LISTE_OFFSET_1 = 0x14;
            public const int PAZAR_LISTE_OFFSET_2 = 0x08;
            public const int PAZAR_LISTE_OFFSET_3 = 0x30;

            public const int PAZAR_SLOT_BOYUTU = 0x78;
            public const int PAZAR_MAX_SLOT = 40;

            public const int OFFSET_ITEM_ADI = 0x00;
            public const int OFFSET_ITEM_FIYAT = 0x44;
            public const int OFFSET_ITEM_MIKTAR = 0x48;
            public const int OFFSET_PAZAR_ID = 0x50;
            public const int OFFSET_SATICI_ID = 0x58;
            public const int OFFSET_SATICI_ADI = 0x60;

            public const long KARAKTER_BASE = 0x00D4A8B0;
            public const int OFFSET_KARAKTER_X = 0x10;
            public const int OFFSET_KARAKTER_Y = 0x14;
            public const int OFFSET_KARAKTER_HP = 0x1C;
            public const int OFFSET_KARAKTER_ENVANTER = 0x2C;
        }

        public bool Baglani(string prosesAdi)
        {
            try
            {
                var processes = Process.GetProcessesByName(prosesAdi);
                if (processes.Length == 0) return false;

                _process = processes[0];
                _handle = OpenProcess(PROCESS_ALL_ACCESS, false, _process.Id);
                if (_handle == IntPtr.Zero) return false;

                _baseAdres = _process.MainModule?.BaseAddress ?? IntPtr.Zero;
                return _baseAdres != IntPtr.Zero;
            }
            catch { return false; }
        }

        public byte[] BellegiOku(IntPtr adres, int boyut)
        {
            byte[] buffer = new byte[boyut];
            ReadProcessMemory(_handle, adres, buffer, boyut, out _);
            return buffer;
        }

        public byte[] OffsetOku(long offset, int boyut)
        {
            IntPtr adres = IntPtr.Add(_baseAdres, (int)offset);
            return BellegiOku(adres, boyut);
        }

        public int IntOku(IntPtr adres)
        {
            return BitConverter.ToInt32(BellegiOku(adres, 4), 0);
        }

        public int IntOffsetOku(long offset)
        {
            return BitConverter.ToInt32(OffsetOku(offset, 4), 0);
        }

        public long LongOku(IntPtr adres)
        {
            return BitConverter.ToInt64(BellegiOku(adres, 8), 0);
        }

        public float FloatOku(IntPtr adres)
        {
            return BitConverter.ToSingle(BellegiOku(adres, 4), 0);
        }

        public string StringOku(IntPtr adres, int maxUzunluk = 64)
        {
            var buf = BellegiOku(adres, maxUzunluk);
            return Encoding.UTF8.GetString(buf).Split('\0')[0];
        }

        public string StringOffsetOku(long offset, int maxUzunluk = 64)
        {
            var buf = OffsetOku(offset, maxUzunluk);
            return Encoding.UTF8.GetString(buf).Split('\0')[0];
        }

        public IntPtr PointerCoz(long baseOffset, int[] offsetler)
        {
            IntPtr adres = IntPtr.Add(_baseAdres, (int)baseOffset);
            byte[] buf = BellegiOku(adres, 4);
            adres = new IntPtr(BitConverter.ToInt32(buf, 0));

            for (int i = 0; i < offsetler.Length - 1; i++)
            {
                buf = BellegiOku(IntPtr.Add(adres, offsetler[i]), 4);
                adres = new IntPtr(BitConverter.ToInt32(buf, 0));
                if (adres == IntPtr.Zero) return IntPtr.Zero;
            }

            return IntPtr.Add(adres, offsetler[^1]);
        }

        public List<Models.MarketItem> PazarListesiOku()
        {
            var liste = new List<Models.MarketItem>();

            try
            {
                IntPtr listeBase = PointerCoz(
                    Adresler.PAZAR_LISTE_POINTER,
                    new[] {
                        Adresler.PAZAR_LISTE_OFFSET_1,
                        Adresler.PAZAR_LISTE_OFFSET_2,
                        Adresler.PAZAR_LISTE_OFFSET_3
                    }
                );

                if (listeBase == IntPtr.Zero) return liste;

                for (int i = 0; i < Adresler.PAZAR_MAX_SLOT; i++)
                {
                    IntPtr slotAdres = IntPtr.Add(listeBase, i * Adresler.PAZAR_SLOT_BOYUTU);

                    string itemAdi = StringOku(
                        IntPtr.Add(slotAdres, Adresler.OFFSET_ITEM_ADI), 64);

                    if (string.IsNullOrWhiteSpace(itemAdi)) continue;

                    int fiyat = IntOku(IntPtr.Add(slotAdres, Adresler.OFFSET_ITEM_FIYAT));
                    int miktar = IntOku(IntPtr.Add(slotAdres, Adresler.OFFSET_ITEM_MIKTAR));
                    int pazarId = IntOku(IntPtr.Add(slotAdres, Adresler.OFFSET_PAZAR_ID));
                    int saticiId = IntOku(IntPtr.Add(slotAdres, Adresler.OFFSET_SATICI_ID));

                    if (fiyat <= 0 || pazarId <= 0) continue;

                    liste.Add(new Models.MarketItem
                    {
                        ItemAdi = itemAdi,
                        Fiyat = fiyat,
                        Miktar = miktar,
                        PazarId = pazarId,
                        SaticiId = saticiId,
                        TespitZamani = DateTime.Now
                    });
                }
            }
            catch { }

            return liste;
        }

        public (float X, float Y) KarakterKonumOku()
        {
            try
            {
                IntPtr adres = IntPtr.Add(_baseAdres, (int)Adresler.KARAKTER_BASE);
                float x = FloatOku(IntPtr.Add(adres, Adresler.OFFSET_KARAKTER_X));
                float y = FloatOku(IntPtr.Add(adres, Adresler.OFFSET_KARAKTER_Y));
                return (x, y);
            }
            catch { return (0, 0); }
        }

        public int KarakterHpOku()
        {
            try
            {
                IntPtr adres = IntPtr.Add(_baseAdres, (int)Adresler.KARAKTER_BASE);
                return IntOku(IntPtr.Add(adres, Adresler.OFFSET_KARAKTER_HP));
            }
            catch { return 0; }
        }

        public List<IntPtr> PatternTara(byte[] pattern, string mask)
        {
            var sonuclar = new List<IntPtr>();
            GetSystemInfo(out SYSTEM_INFO sysInfo);

            IntPtr adres = sysInfo.lpMinimumApplicationAddress;
            IntPtr maxAdres = sysInfo.lpMaximumApplicationAddress;

            while (adres.ToInt64() < maxAdres.ToInt64())
            {
                if (VirtualQueryEx(_handle, adres, out MEMORY_BASIC_INFORMATION mbi,
                    (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0) break;

                bool yazilabilir = (mbi.Protect & PAGE_READWRITE) != 0 ||
                                   (mbi.Protect & PAGE_WRITECOPY) != 0 ||
                                   (mbi.Protect & PAGE_EXECUTE_READWRITE) != 0 ||
                                   (mbi.Protect & PAGE_EXECUTE_WRITECOPY) != 0;

                if (mbi.State == MEM_COMMIT && yazilabilir)
                {
                    int boyut = mbi.RegionSize.ToInt32();
                    byte[] buffer = new byte[boyut];
                    ReadProcessMemory(_handle, adres, buffer, boyut, out int okunan);

                    for (int i = 0; i < okunan - pattern.Length; i++)
                    {
                        bool eslesme = true;
                        for (int j = 0; j < pattern.Length; j++)
                        {
                            if (mask[j] == '?' ) continue;
                            if (buffer[i + j] != pattern[j]) { eslesme = false; break; }
                        }
                        if (eslesme)
                            sonuclar.Add(IntPtr.Add(adres, i));
                    }
                }

                adres = IntPtr.Add(adres, mbi.RegionSize.ToInt32());
            }

            return sonuclar;
        }

        public bool BagliMi() =>
            _process != null && !_process.HasExited && _handle != IntPtr.Zero;

        public IntPtr BaseAdres => _baseAdres;
        public Process? Proses => _process;

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
                CloseHandle(_handle);
            _process?.Dispose();
        }
    }
}