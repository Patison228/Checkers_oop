using CheckersClient.Views;
using System.Windows;

namespace CheckersClient.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        public RelayCommand CreateRoomCommand { get; }
        public RelayCommand JoinRoomCommand { get; }

        public MainMenuViewModel()
        {
            CreateRoomCommand = new RelayCommand(_ => OpenCreateRoom());
            JoinRoomCommand = new RelayCommand(_ => OpenJoinRoom());
        }

        private void OpenCreateRoom()
        {
            var window = new CreateRoomWindow();
            window.Show();
            Application.Current.MainWindow.Close();
        }

        private void OpenJoinRoom()
        {
            var window = new JoinRoomWindow();
            window.Show();
            Application.Current.MainWindow.Close();
        }
    }
}
