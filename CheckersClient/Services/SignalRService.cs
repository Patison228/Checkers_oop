using CheckersModels.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace CheckersClient.Services
{
    /// <summary>
    /// Обёртка над SignalR-подключением клиента к хабу шашек.
    /// Отвечает за установку соединения, подписку на события и отправку команд на сервер.
    /// </summary>
    public class SignalRService
    {
        /// <summary>
        /// Внутреннее подключение SignalR к хабу.
        /// </summary>
        private HubConnection _connection;

        /// <summary>
        /// Событие вызывается при успешном создании комнаты на сервере.
        /// Первый параметр — идентификатор комнаты, второй — начальное состояние игры.
        /// </summary>
        public event Action<string, GameState>? RoomCreated;

        /// <summary>
        /// Событие начала игры, вызывается для обоих игроков, когда к комнате подключились двое.
        /// Содержит текущее состояние игры.
        /// </summary>
        public event Action<GameState>? GameStarted;

        /// <summary>
        /// Событие обновления состояния игры.
        /// Вызывается каждый раз, когда на сервере успешно применён ход.
        /// </summary>
        public event Action<GameState>? StateUpdated;

        /// <summary>
        /// Событие окончания игры.
        /// Параметр — строка с цветом победителя.
        /// </summary>
        public event Action<string>? GameOver;

        /// <summary>
        /// Событие неудачного подключения к комнате.
        /// Параметр — текст ошибки от сервера.
        /// </summary>
        public event Action<string>? JoinFailed;

        /// <summary>
        /// Событие отклонённого хода.
        /// Параметр — текст причины, по которой ход был признан недопустимым.
        /// </summary>
        public event Action<string>? MoveRejected;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SignalRService"/> и настраивает обработчики событий хаба.
        /// Создаёт подключение к серверу по указанному URL, но не запускает его.
        /// </summary>
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

        /// <summary>
        /// Устанавливает соединение с SignalR-хабом.
        /// Должен быть вызван один раз при запуске клиента перед отправкой любых команд.
        /// </summary>
        public async Task ConnectAsync()
        {
            await _connection.StartAsync();
        }

        /// <summary>
        /// Отправляет на сервер команду создания новой комнаты для игры.
        /// После успешного выполнения сервер должен вызвать событие <see cref="RoomCreated"/>.
        /// </summary>
        public async Task SendCreateRoom() => await _connection.SendAsync("CreateRoom");

        /// <summary>
        /// Отправляет запрос на подключение к существующей комнате по её идентификатору.
        /// При успехе сервер вызовет <see cref="GameStarted"/>, при ошибке — <see cref="JoinFailed"/>.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты, к которой нужно подключиться.</param>
        public async Task SendJoinRoom(string roomId) => await _connection.SendAsync("JoinRoom", roomId);

        /// <summary>
        /// Отправляет на сервер запрос совершить ход в указанной комнате.
        /// В случае успеха сервер разошлёт новое состояние через событие <see cref="StateUpdated"/>.
        /// </summary>
        /// <param name="move">Данные хода: комната, координаты исходной и целевой клетки.</param>
        public async Task SendMakeMove(MoveRequest move) => await _connection.SendAsync("MakeMove", move);
    }
}
