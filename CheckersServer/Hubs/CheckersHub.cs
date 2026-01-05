using System.Threading.Tasks;
using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR;

namespace CheckersServer.Hubs
{
    public class CheckersHub : Hub
    {
        private readonly GameManager _gameManager;

        public CheckersHub(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        // Создать комнату, вернуть RoomId и стартовое состояние
        public async Task CreateRoom()
        {
            var connectionId = Context.ConnectionId;

            var game = _gameManager.CreateRoom(connectionId);

            await Groups.AddToGroupAsync(connectionId, game.RoomId);

            await Clients.Caller.SendAsync("RoomCreated", game.RoomId, game);
        }

        // Подключиться к существующей комнате по RoomId
        public async Task JoinRoom(string roomId)
        {
            var connectionId = Context.ConnectionId;

            var game = _gameManager.JoinRoom(roomId, connectionId);
            if (game == null)
            {
                await Clients.Caller.SendAsync("JoinFailed", "Комната не найдена или уже занята");
                return;
            }

            await Groups.AddToGroupAsync(connectionId, roomId);

            // Уведомляем обоих игроков полным состоянием
            await Clients.Group(roomId).SendAsync("GameStarted", game);
        }

        // Получение текущего состояния (на случай переподключения)
        public Task GetState(string roomId)
        {
            var game = _gameManager.GetGame(roomId);
            if (game != null)
            {
                return Clients.Caller.SendAsync("StateUpdated", game);
            }

            return Clients.Caller.SendAsync("JoinFailed", "Комната не найдена");
        }

        // Ход игрока
        public async Task MakeMove(MoveRequest move)
        {
            var connectionId = Context.ConnectionId;

            if (_gameManager.TryApplyMove(connectionId, move, out var updated) && updated != null)
            {
                // Рассылаем новое состояние всем в комнате
                await Clients.Group(move.RoomId).SendAsync("StateUpdated", updated);

                if (updated.IsGameOver)
                {
                    await Clients.Group(move.RoomId)
                        .SendAsync("GameOver", updated.Winner);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("MoveRejected", "Недопустимый ход");
            }
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Минимальный вариант: просто уведомить группу, что кто-то вышел
            await base.OnDisconnectedAsync(exception);
            // При желании можно здесь помечать игру как завершённую
        }
    }
}
