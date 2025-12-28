using CheckersModels.GameLogic;
using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace CheckersServer.Hubs
{
    public class CheckersHub : Hub
    {
        private static readonly ConcurrentDictionary<string, GameState> _rooms = new();

        public async Task CreateRoom(string playerName)
        {
            try
            {
                Console.WriteLine($"🔵 CreateRoom: {playerName}");
                var roomId = Guid.NewGuid().ToString()[..8].ToUpper();

                var gameState = new GameState
                {
                    RoomId = roomId,
                    Player1 = playerName
                };

                GameEngine.InitializeBoard(gameState);
                _rooms[roomId] = gameState;

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                Console.WriteLine($"✅ Комната {roomId} создана");

                await Clients.Caller.SendAsync("RoomCreated", roomId, gameState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task JoinRoom(string roomId, string playerName)
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var gameState))
                {
                    await Clients.Caller.SendAsync("Error", "Комната не найдена");
                    return;
                }

                if (!string.IsNullOrEmpty(gameState.Player2))
                {
                    await Clients.Caller.SendAsync("Error", "Комната полная");
                    return;
                }

                gameState.Player2 = playerName;
                gameState.IsGameStarted = true;

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("PlayerJoined", playerName, gameState);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task MakeMove(string roomId, Move move)
        {
            try
            {
                if (_rooms.TryGetValue(roomId, out var gameState) &&
                    GameEngine.IsValidMove(gameState, move))
                {
                    GameEngine.MakeMove(gameState, move);
                    await Clients.Group(roomId).SendAsync("BoardUpdated", gameState);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", "Недопустимый ход");
            }
        }
    }
}
