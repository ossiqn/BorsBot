using System.Windows;

namespace BorsaBot
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) =>
            {
                if (DataContext is ViewModels.MainViewModel vm)
                    vm.Dispose();
            };
        }
    }
}