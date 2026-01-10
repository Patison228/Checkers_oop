using CheckersClient.Services;
using CheckersClient.Views;
using CheckersModels.Models;
using System.ComponentModel;
using System.Windows;

namespace CheckersClient.ViewModels
{
    public class CreateRoomViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private string _status = "Создаю комнату...";
        private string? _roomId;

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string? RoomId => _roomId;

        public CreateRoomViewModel(SignalRService signalRService)
        {
            _signalRService = signalRService;
            _signalRService.RoomCreated += OnRoomCreated;
            _signalRService.GameStarted += OnGameStarted;
            CreateRoomAsync();
        }

        private async void CreateRoomAsync()
        {
            await _signalRService.SendCreateRoom();
        }

        private void OnRoomCreated(string roomId, GameState state)
        {
            _roomId = roomId;
            Status = $"Комната {roomId} создана. Жду второго игрока...";
        }

        private void OnGameStarted(GameState state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var gameWindow = new GameWindow();
                gameWindow.DataContext = new GameViewModel(_signalRService, state);
                gameWindow.Show();
            });
        }
    }
}
