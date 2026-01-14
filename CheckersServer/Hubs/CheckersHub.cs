using System.Threading.Tasks;
using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR;

namespace CheckersServer.Hubs
{
    /// <summary>
    /// SignalR‑хаб для управления онлайн-партиями шашек.
    /// Обрабатывает создание и подключение к комнатам, ходы игроков и рассылку состояния игры.
    /// </summary>
    public class CheckersHub : Hub
    {
        /// <summary>
        /// Сервис управления партиями, содержащий всю игровую логику и состояния комнат.
        /// </summary>
        private readonly GameManager _gameManager;

        /// <summary>
        /// Инициализирует новый экземпляр хаба шашек с указанным менеджером игр.
        /// </summary>
        /// <param name="gameManager">Экземпляр <see cref="GameManager"/>, управляющий состояниями игр.</param>
        public CheckersHub(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// Создаёт новую комнату для игры, добавляет вызывающего клиента в соответствующую группу
        /// и отправляет ему идентификатор комнаты и стартовое состояние доски.
        /// </summary>
        public async Task CreateRoom()
        {
            var connectionId = Context.ConnectionId;

            var game = _gameManager.CreateRoom(connectionId);

            await Groups.AddToGroupAsync(connectionId, game.RoomId);

            await Clients.Caller.SendAsync("RoomCreated", game.RoomId, game);
        }

        /// <summary>
        /// Пытается подключить клиента к существующей комнате по её идентификатору.
        /// При успехе добавляет клиента в группу и рассылает обоим игрокам полное состояние игры.
        /// В случае ошибки отправляет вызывающему сообщение <c>JoinFailed</c>.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты, к которой нужно подключиться.</param>
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

            await Clients.Group(roomId).SendAsync("GameStarted", game);
        }

        /// <summary>
        /// Возвращает вызывающему клиенту актуальное состояние игры указанной комнаты.
        /// Может использоваться при переподключении или обновлении клиента.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты, состояние которой требуется получить.</param>
        public Task GetState(string roomId)
        {
            var game = _gameManager.GetGame(roomId);
            if (game != null)
            {
                return Clients.Caller.SendAsync("StateUpdated", game);
            }

            return Clients.Caller.SendAsync("JoinFailed", "Комната не найдена");
        }

        /// <summary>
        /// Обрабатывает запрос на совершение хода от клиента.
        /// При успешной валидации и применении хода рассылает обновлённое состояние всем игрокам комнаты,
        /// а при завершении игры дополнительно отправляет сообщение <c>GameOver</c> с победителем.
        /// В случае недопустимого хода отвечает вызывающему <c>MoveRejected</c>.
        /// </summary>
        /// <param name="move">Запрос хода с координатами исходной и целевой клетки, а также идентификатором комнаты.</param>
        public async Task MakeMove(MoveRequest move)
        {
            var connectionId = Context.ConnectionId;

            if (_gameManager.TryApplyMove(connectionId, move, out var updated) && updated != null)
            {
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

        /// <summary>
        /// Вызывается при отключении клиента от хаба.
        /// В текущей реализации только вызывает базовую логику, но может быть расширен
        /// для пометки игры как завершённой или уведомления второго игрока.
        /// </summary>
        /// <param name="exception">Исключение, приведшее к отключению, если есть.</param>
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
