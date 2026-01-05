using CheckersClient.Services;
using CheckersClient.Views;
using System.Threading.Tasks;
using System.Windows;

namespace CheckersClient
{
    public partial class App : Application
    {
        private SignalRService _signalRService = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            _signalRService = new SignalRService();

            // Подключаемся к серверу при запуске
            try
            {
                await _signalRService.ConnectAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}\nУбедитесь, что сервер запущен на https://localhost:5001",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }
}
