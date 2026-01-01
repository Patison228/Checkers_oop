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
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string, GameState>("RoomCreated",
                (roomId, state) => RoomCreated?.Invoke(roomId, state));

            _connection.On<string, GameState>("PlayerJoined",
                (player, state) => PlayerJoined?.Invoke(player, state));

            _connection.On<GameState>("BoardUpdated",
                state => BoardUpdated?.Invoke(state));

            _connection.On<string>("Error",
                error => ErrorReceived?.Invoke(error));

            await _connection.StartAsync();
        }

        public Task CreateRoomAsync(string playerName) =>
            _connection.InvokeAsync("CreateRoom", playerName);

        public Task JoinRoomAsync(string roomId, string playerName) =>
            _connection.InvokeAsync("JoinRoom", roomId, playerName);

        public Task MakeMoveAsync(string roomId, Move move) =>
            _connection.InvokeAsync("MakeMove", roomId, move);
    }
}
