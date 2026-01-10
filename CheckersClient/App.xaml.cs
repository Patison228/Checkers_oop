using CheckersClient.Services;
using CheckersClient.ViewModels;
using CheckersClient.Views;
using System.Threading.Tasks;
using System.Windows;

namespace CheckersClient
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            var signalRService = new SignalRService();

            try
            {
                await signalRService.ConnectAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}\nУбедитесь, что сервер запущен на https://localhost:7026",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainVM = new MainMenuViewModel(signalRService);
            var mainWindow = new MainMenuWindow
            {
                DataContext = mainVM
            };

            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
