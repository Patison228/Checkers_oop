using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Windows.Threading;

namespace CheckersClient.Services
{
    public class SignalRService
    {
        private HubConnection _connection;
        public string CurrentRoomId { get; private set; }

        public event Action<string, GameState> RoomCreated;
        public event Action<string, GameState> PlayerJoined;
        public event Action<GameState> BoardUpdated;
        public event Action<string> ErrorReceived;

        public async Task ConnectAsync(string hubUrl)
        {
            try
            {
                Console.WriteLine($"🔌 Подключение к {hubUrl}...");
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // ✅ ТОЧНЫЕ сигнатуры сервера
                _connection.On<string, GameState>("RoomCreated", (roomId, state) =>
                {
                    Console.WriteLine($"📨 RoomCreated: {roomId}");
                    RoomCreated?.Invoke(roomId, state);
                });

                _connection.On<string, GameState>("PlayerJoined", (playerName, state) =>
                {
                    Console.WriteLine($"📨 PlayerJoined: {playerName}");
                    PlayerJoined?.Invoke(playerName, state);
                });

                _connection.Closed += async error =>
                {
                    Console.WriteLine($"🔌 SignalR отключен: {error?.Message}");
                    await Task.CompletedTask;
                };

                await _connection.StartAsync();
                Console.WriteLine("✅ SignalR подключен!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SignalR ошибка: {ex}");
                ErrorReceived?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task CreateRoomAsync(string playerName)
        {
            if (_connection?.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Не подключен к серверу");

            Console.WriteLine($"🏠 Создание комнаты для {playerName}");
            await _connection.InvokeAsync("CreateRoom", playerName);
        }

        public async Task JoinRoomAsync(string roomId, string playerName)
        {
            Console.WriteLine($"🔗 Подключение к {roomId}");
            await _connection.InvokeAsync("JoinRoom", roomId, playerName);
        }

        public async Task MakeMoveAsync(string roomId, Move move)
        {
            await _connection.InvokeAsync("MakeMove", roomId, move);
        }

        public void Dispose()
        {
            _connection?.DisposeAsync();
        }
    }
}
