using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BorsaBot.Core;
using BorsaBot.Models;

namespace BorsaBot.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private BotEngine? _engine;
        private readonly BotConfig _config = new();

        public ObservableCollection<string> LogMesajlari { get; } = new();
        public ObservableCollection<string> SinyalListesi { get; } = new();

        private bool _calisiyor;
        public bool Calisiyor
        {
            get => _calisiyor;
            set { _calisiyor = value; OnPropertyChanged(); OnPropertyChanged(nameof(BaslatButonAktif)); OnPropertyChanged(nameof(DurdurButonAktif)); }
        }

        public bool BaslatButonAktif => !Calisiyor;
        public bool DurdurButonAktif => Calisiyor;

        private string _durum = "Hazir";
        public string Durum { get => _durum; set { _durum = value; OnPropertyChanged(); } }

        private int _toplamTarama;
        public int ToplamTarama { get => _toplamTarama; set { _toplamTarama = value; OnPropertyChanged(); } }

        private int _toplamAlis;
        public int ToplamAlis { get => _toplamAlis; set { _toplamAlis = value; OnPropertyChanged(); } }

        private string _toplamKar = "0";
        public string ToplamKar { get => _toplamKar; set { _toplamKar = value; OnPropertyChanged(); } }

        public string OyunProsesAdi { get => _config.OyunProsesAdi; set { _config.OyunProsesAdi = value; OnPropertyChanged(); } }
        public int TaramaHizi { get => _config.TaramaHiziMs; set { _config.TaramaHiziMs = value; OnPropertyChanged(); } }
        public int MinKarMarji { get => _config.MinKarMarji; set { _config.MinKarMarji = value; OnPropertyChanged(); } }
        public bool AntiDetect { get => _config.AntiDetect; set { _config.AntiDetect = value; OnPropertyChanged(); } }
        public bool OtomatikSatis { get => _config.OtomatikSatis; set { _config.OtomatikSatis = value; OnPropertyChanged(); } }

        public ICommand BotBaslatCommand { get; }
        public ICommand BotDurdurCommand { get; }
        public ICommand LogTemizleCommand { get; }

        public MainViewModel()
        {
            BotBaslatCommand = new RelayCommand(BotBaslat, () => !Calisiyor);
            BotDurdurCommand = new RelayCommand(BotDurdur, () => Calisiyor);
            LogTemizleCommand = new RelayCommand(() => LogMesajlari.Clear());
        }

        private async void BotBaslat()
        {
            _engine?.Dispose();
            _engine = new BotEngine(_config);

            _engine.LogEvent += mesaj =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogMesajlari.Add(mesaj);
                    if (LogMesajlari.Count > 500)
                        LogMesajlari.RemoveAt(0);
                });
            };

            _engine.SinyalEvent += (item, sinyal) =>
            {
                if (sinyal.Aksiyon != Aksiyon.BEKLE)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SinyalListesi.Insert(0, $"[{sinyal.Aksiyon}] {item.ItemAdi} | {item.Fiyat:N0}");
                        if (SinyalListesi.Count > 100) SinyalListesi.RemoveAt(SinyalListesi.Count - 1);
                        ToplamAlis = _engine.ToplamAlis;
                        ToplamKar = $"{_engine.ToplamKar:N0}";
                    });
                }
            };

            bool basarili = await _engine.Baslat();
            if (basarili)
            {
                Calisiyor = true;
                Durum = "Calisiyor";
                _ = IstatistikGuncelleDongusu();
            }
        }

        private void BotDurdur()
        {
            _engine?.Durdur();
            Calisiyor = false;
            Durum = "Durduruldu";
        }

        private async Task IstatistikGuncelleDongusu()
        {
            while (Calisiyor)
            {
                if (_engine != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ToplamTarama = _engine.ToplamTarama;
                        ToplamAlis = _engine.ToplamAlis;
                        ToplamKar = $"{_engine.ToplamKar:N0}";
                    });
                }
                await Task.Delay(1000);
            }
        }

        public void Dispose() => _engine?.Dispose();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}