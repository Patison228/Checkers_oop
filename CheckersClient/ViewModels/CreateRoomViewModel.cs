using CheckersClient.Services;
using CheckersClient.Views;
using CheckersModels.Models;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CheckersClient.ViewModels
{
    public class CreateRoomViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private string _status = "Создание комнаты...";
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

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                Status = "Подключение к серверу...";
                await _signalRService.ConnectAsync("https://localhost:7026/checkersHub");

                Status = "Создание комнаты...";
                await _signalRService.CreateRoomAsync(PlayerName);
            }
            catch (Exception ex)
            {
                Status = $"Ошибка подключения: {ex.Message}";
                Console.WriteLine(ex);
            }
        }

        private void OnRoomCreated(string roomId, GameState state)
        {
            _roomId = roomId;
            Status = $"Комната {roomId} создана. Ожидание второго игрока...";
            OnPropertyChanged(nameof(RoomIdDisplay));
            Console.WriteLine($"RoomCreated: {roomId}");
        }

        private void OnPlayerJoined(string playerName, GameState state)
        {
            Console.WriteLine($"PlayerJoined: {playerName}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                Status = $"Игрок {playerName} подключился. Игра началась!";
                OpenGameWindow(state);
            });
        }

        private void OnError(string error)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Status = $"Ошибка: {error}";
            });
            Console.WriteLine($"Error from server: {error}");
        }

        private void OpenGameWindow(GameState gameState)
        {

            var gameVm = new GameViewModel
            {
                SignalRService = _signalRService,
                GameState = gameState
            };

            var gameWindow = new GameWindow
            {
                DataContext = gameVm
            };
            gameWindow.Show();
        }
    }
}
