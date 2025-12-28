using CheckersClient.Services;
using CheckersClient.Views;
using CheckersModels.Models;
using System.Windows;

namespace CheckersClient.ViewModels
{
    public class JoinRoomViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private string _roomId = "";
        private string _playerName = "Игрок2";
        private string _status = "Введите ID комнаты";

        public string RoomId
        {
            get => _roomId;
            set => SetProperty(ref _roomId, value.ToUpper());
        }

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public RelayCommand JoinCommand { get; }

        public JoinRoomViewModel()
        {
            _signalRService = new SignalRService();
            JoinCommand = new RelayCommand(_ => JoinRoom(), _ => !string.IsNullOrWhiteSpace(RoomId));
        }

        private async void JoinRoom()
        {
            Status = "Подключение...";
            await _signalRService.ConnectAsync("https://localhost:7026/checkersHub");
            await _signalRService.JoinRoomAsync(RoomId, PlayerName);
        }
    }
}
