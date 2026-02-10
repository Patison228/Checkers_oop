using CheckersClient.Services;
using CheckersClient.Views;
using CheckersModels.Models;
using System.ComponentModel;
using System.Windows;

namespace CheckersClient.ViewModels
{
    public class JoinRoomViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private string _roomId = "";
        private string _status = "Введите ID комнаты:";

        public string RoomId
        {
            get => _roomId;
            set { _roomId = value.ToUpper(); OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public JoinRoomViewModel(SignalRService signalRService)
        {
            _signalRService = signalRService;
            _signalRService.GameStarted += OnGameStarted;
            _signalRService.JoinFailed += OnJoinFailed;
        }

        public void Join()
        {
            if (string.IsNullOrWhiteSpace(RoomId))
            {
                Status = "Введите ID комнаты";
                return;
            }

            Status = "Подключаюсь...";
            _signalRService.SendJoinRoom(RoomId);
        }

        private void OnGameStarted(GameState state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var gameWindow = new GameWindow();
                gameWindow.DataContext = new GameViewModel(_signalRService, state, "Black");
                gameWindow.Show();
            });
        }

        private void OnJoinFailed(string message)
        {
            Status = $"Ошибка: {message}";
        }
    }
}
