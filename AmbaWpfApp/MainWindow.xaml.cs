using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AmbaSimpleClass;

namespace AmbaWpfApp
{
    public partial class MainWindow : Window
    {
        private readonly AmbaService _service;
        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            _service = new AmbaService(LogMessage);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                LogMessage("⚠️ Сервис уже запущен!");
                return;
            }

            _isRunning = true;
            StartButton.Content = "🔄 ИДЁТ СИНХРОНИЗАЦИЯ...";
            StartButton.Background = System.Windows.Media.Brushes.DarkGray;

            await Task.Run(() =>
            {
                _service.Start();
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                LogMessage("⚠️ Сервис уже остановлен.");
                return;
            }

            _service.Stop();
            _isRunning = false;
            StartButton.Content = "▶️ СТАРТ";
            StartButton.Background = System.Windows.Media.Brushes.Green;
        }

        private void LogMessage(string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                LogText.AppendText($"\n{DateTime.Now:HH:mm:ss} {message}");

                LogText.CaretIndex = LogText.Text.Length;
                LogText.ScrollToEnd();
            }, DispatcherPriority.Render);
        }
    }
}
