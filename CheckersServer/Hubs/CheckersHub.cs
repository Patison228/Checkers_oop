using CheckersModels.GameLogic;
using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CheckersServer.Hubs
{
    public class CheckersHub : Hub
    {
        // ✅ Thread-safe ConcurrentDictionary
        private static readonly ConcurrentDictionary<string, GameState> _rooms = new();
        private static readonly ConcurrentDictionary<string, HashSet<string>> _roomConnections = new();

        public async Task CreateRoom(string playerName)
        {
            try
            {
                Console.WriteLine($"🔵 CreateRoom вызван: {playerName}");

                var roomId = Guid.NewGuid().ToString()[..8].ToUpper();
                var gameState = new GameState
                {
                    RoomId = roomId,
                    Player1 = playerName,
                    IsGameStarted = false
                };

                // ✅ Инициализация доски
                GameEngine.InitializeBoard(gameState);
                _rooms[roomId] = gameState;
                _roomConnections[roomId] = new HashSet<string>();

                // ✅ Добавляем клиента в группу
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                _roomConnections[roomId].Add(Context.ConnectionId);

                Console.WriteLine($"✅ Комната создана: {roomId} для {playerName}");

                // ✅ Отправляем ответ ТОЛЬКО создателю
                await Clients.Caller.SendAsync("RoomCreated", roomId, gameState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка CreateRoom: {ex}");
                await Clients.Caller.SendAsync("Error", $"Ошибка создания комнаты: {ex.Message}");
            }
        }

        public async Task JoinRoom(string roomId, string playerName)
        {
            try
            {
                Console.WriteLine($"🔗 JoinRoom: {roomId} / {playerName}");

                if (!_rooms.TryGetValue(roomId, out var gameState))
                {
                    await Clients.Caller.SendAsync("Error", "Комната не найдена");
                    return;
                }

                if (!string.IsNullOrEmpty(gameState.Player2))
                {
                    await Clients.Caller.SendAsync("Error", "Комната уже полная");
                    return;
                }

                // ✅ Второй игрок подключился
                gameState.Player2 = playerName;
                gameState.IsGameStarted = true;

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                _roomConnections[roomId].Add(Context.ConnectionId);

                Console.WriteLine($"✅ Игрок 2 подключился: {playerName} в {roomId}");

                // ✅ Отправляем всем в комнате
                await Clients.Group(roomId).SendAsync("PlayerJoined", playerName, gameState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка JoinRoom: {ex}");
                await Clients.Caller.SendAsync("Error", $"Ошибка подключения: {ex.Message}");
            }
        }

        public async Task MakeMove(string roomId, Move move)
        {
            try
            {
                Console.WriteLine($"♟️ MakeMove в {roomId}: {move.FromRow},{move.FromCol} → {move.ToRow},{move.ToCol}");

                if (!_rooms.TryGetValue(roomId, out var gameState))
                {
                    await Clients.Caller.SendAsync("Error", "Комната не найдена");
                    return;
                }

                if (GameEngine.IsValidMove(gameState, move))
                {
                    GameEngine.MakeMove(gameState, move);
                    await Clients.Group(roomId).SendAsync("BoardUpdated", gameState);
                    Console.WriteLine($"✅ Ход выполнен в {roomId}");
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Недопустимый ход");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка MakeMove: {ex}");
                await Clients.Caller.SendAsync("Error", "Ошибка хода");
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // ✅ Удаляем соединение из всех комнат
            foreach (var roomConnections in _roomConnections)
            {
                if (roomConnections.Value.Remove(Context.ConnectionId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomConnections.Key);
                    Console.WriteLine($"🔌 {Context.ConnectionId} отключился из {roomConnections.Key}");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 🔍 Debug эндпоинт
        public async Task GetActiveRooms()
        {
            var rooms = _rooms.Count;
            await Clients.Caller.SendAsync("DebugInfo", $"Активных комнат: {rooms}");
        }
    }
}
