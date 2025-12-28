using CheckersClient.Services;
using CheckersClient.Views;
using CheckersModels.Models;
using System.ComponentModel;
using System.Windows;

namespace CheckersClient.ViewModels
{
    public class CreateRoomViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly SignalRService _signalRService;
        private string _status = "Ожидание противника...";
        private string _roomId;
        private string _playerName = "Игрок1";

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string RoomIdDisplay => _roomId ?? "Генерация...";
        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public CreateRoomViewModel()
        {
            _signalRService = new SignalRService();
            _signalRService.RoomCreated += OnRoomCreated;
            _signalRService.PlayerJoined += OnPlayerJoined;
            _signalRService.ErrorReceived += OnError;

            ConnectAndCreateRoom();
        }

        private async void ConnectAndCreateRoom()
        {
            await _signalRService.ConnectAsync("https://localhost:5001/checkersHub");
            await _signalRService.CreateRoomAsync(PlayerName);
        }

        private void OnRoomCreated(string roomId, GameState gameState)
        {
            _roomId = roomId;
            Status = "Комната создана. Ожидание противника...";
            OnPropertyChanged(nameof(RoomIdDisplay));
        }

        private void OnPlayerJoined(string playerName, GameState gameState)
        {
            Status = "Игра началась!";
            OpenGameWindow(gameState);  // Теперь работает!
        }

        private void OnError(string error)
        {
            Status = $"Ошибка: {error}";
        }

        private void OpenGameWindow(GameState gameState)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var gameVM = new GameViewModel()
                {
                    GameState = gameState,
                    SignalRService = _signalRService
                };

                var gameWindow = new GameWindow();
                gameWindow.DataContext = gameVM;  // ← КЛЮЧЕВОЕ ИЗМЕНЕНИЕ
                gameWindow.Show();
                Application.Current.MainWindow.Close();
            });
        }

    }
}
