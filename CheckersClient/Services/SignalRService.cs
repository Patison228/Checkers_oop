using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace CheckersClient.Services
{
    public class SignalRService
    {
        private HubConnection _connection;
        public event Action<string, GameState>? RoomCreated;
        public event Action<GameState>? GameStarted;
        public event Action<GameState>? StateUpdated;
        public event Action<string>? GameOver;
        public event Action<string>? JoinFailed;
        public event Action<string>? MoveRejected;

        public SignalRService()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7026/checkersHub")
                .Build();

            _connection.On<string, GameState>("RoomCreated", (roomId, state) => RoomCreated?.Invoke(roomId, state));

            _connection.On<GameState>("GameStarted", state => GameStarted?.Invoke(state));

            _connection.On<GameState>("StateUpdated", state => StateUpdated?.Invoke(state));

            _connection.On<string>("GameOver", winner => GameOver?.Invoke(winner));

            _connection.On<string>("JoinFailed", msg => JoinFailed?.Invoke(msg));

            _connection.On<string>("MoveRejected", msg => MoveRejected?.Invoke(msg));
        }

        public async Task ConnectAsync()
        {
            await _connection.StartAsync();
        }

        public async Task SendCreateRoom() => await _connection.SendAsync("CreateRoom");
        public async Task SendJoinRoom(string roomId) => await _connection.SendAsync("JoinRoom", roomId);
        public async Task SendMakeMove(MoveRequest move) => await _connection.SendAsync("MakeMove", move);
    }
}
