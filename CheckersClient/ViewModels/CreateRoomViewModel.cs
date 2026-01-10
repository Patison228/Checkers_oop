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
    }
}
