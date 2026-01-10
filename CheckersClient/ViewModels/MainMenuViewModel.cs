using CheckersClient.Services;
using CheckersClient.Views;
using System.Windows;

namespace CheckersClient.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;

        public MainMenuViewModel(SignalRService signalRService)
        {
            _signalRService = signalRService;
        }

        public void OpenCreateRoom()
        {
            var win = new CreateRoomWindow
            {
                DataContext = new CreateRoomViewModel(_signalRService)
            };

            win.Show();
        }

        public void OpenJoinRoom()
        {
            var win = new JoinRoomWindow
            {
                DataContext = new JoinRoomViewModel(_signalRService)
            };

            win.Show();
        }
    }
}
