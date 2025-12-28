using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace CheckersClient.Services
{
    public class SignalRService
    {
        private HubConnection _connection;

        public event Action<string, GameState> RoomCreated;
        public event Action<string, GameState> PlayerJoined;
        public event Action<GameState> BoardUpdated;
        public event Action<string> ErrorReceived;

        public async Task ConnectAsync(string hubUrl)
        {
            Console.WriteLine($"🔌 {hubUrl}");
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string, GameState>("RoomCreated", RoomCreated.Invoke);
            _connection.On<string, GameState>("PlayerJoined", (player, state) => PlayerJoined?.Invoke(player, state));
            _connection.On<GameState>("BoardUpdated", BoardUpdated.Invoke);
            _connection.On<string>("Error", ErrorReceived.Invoke);

            await _connection.StartAsync();
            Console.WriteLine("✅ Подключен!");
        }

        public async Task CreateRoomAsync(string playerName)
        {
            await _connection.InvokeAsync("CreateRoom", playerName);
        }

        public async Task JoinRoomAsync(string roomId, string playerName)
        {
            await _connection.InvokeAsync("JoinRoom", roomId, playerName);
        }

        public async Task MakeMoveAsync(string roomId, Move move)
        {
            await _connection.InvokeAsync("MakeMove", roomId, move);
        }
    }
}
