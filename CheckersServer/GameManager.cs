using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CheckersModels.Models;

/// <summary>
/// Менеджер состояния всех активных игр на сервере.
/// Отвечает за создание комнат, подключение игроков и валидацию ходов.
/// Использует потокобезопасную коллекцию для многопользовательской работы.
/// </summary>
namespace CheckersServer
{
    /// <summary>
    /// Основной класс управления партиями шашек.
    /// Хранит активные комнаты в памяти и реализует правила игры.
    /// </summary>
    public class GameManager
    {
        /// <summary>
        /// Потокобезопасная коллекция активных комнат по их идентификаторам.
        /// Ключ — RoomId, значение — полное состояние игры.
        /// </summary>
        private readonly ConcurrentDictionary<string, GameState> _games = new();

        /// <summary>
        /// Создаёт новую комнату для одного игрока и возвращает её состояние.
        /// Генерирует уникальный 8-символьный идентификатор комнаты.
        /// </summary>
        /// <param name="connectionId">Идентификатор SignalR-соединения создателя комнаты.</param>
        /// <returns>Новое состояние игры с пустой доской и ожиданием второго игрока.</returns>
        public GameState CreateRoom(string connectionId)
        {
            var roomId = Guid.NewGuid().ToString("N")[..8].ToUpper();

            var game = new GameState
            {
                RoomId = roomId,
                Player1ConnectionId = connectionId,
                IsGameStarted = false,
                IsGameOver = false,
                CurrentPlayer = "White",
                Board = CreateInitialBoard()
            };

            _games[roomId] = game;
            return game;
        }

        /// <summary>
        /// Подключает второго игрока к существующей комнате и запускает игру.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты для подключения.</param>
        /// <param name="connectionId">Идентификатор SignalR-соединения присоединяющегося игрока.</param>
        /// <returns>Обновлённое состояние игры, если подключение успешно, иначе null.</returns>
        public GameState? JoinRoom(string roomId, string connectionId)
        {
            if (!_games.TryGetValue(roomId, out var game))
                return null;

            if (!string.IsNullOrEmpty(game.Player2ConnectionId))
                return null;

            game.Player2ConnectionId = connectionId;
            game.IsGameStarted = true;

            return game;
        }

        /// <summary>
        /// Возвращает текущее состояние игры по идентификатору комнаты.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты.</param>
        /// <returns>Состояние игры или null, если комната не найдена.</returns>
        public GameState? GetGame(string roomId)
        {
            _games.TryGetValue(roomId, out var game);

            return game;
        }

        /// <summary>
        /// Проверяет допустимость хода и применяет его к состоянию игры, если ход корректен.
        /// </summary>
        /// <param name="connectionId">Идентификатор соединения делающего ход.</param>
        /// <param name="move">Данные хода: координаты "откуда" и "куда".</param>
        /// <param name="updated">Обновлённое состояние игры при успехе.</param>
        /// <returns>true, если ход применён успешно.</returns>
        public bool TryApplyMove(string connectionId, MoveRequest move, out GameState? updated)
        {
            updated = null;

            if (!_games.TryGetValue(move.RoomId, out var game))
                return false;

            var playerColor = game.Player1ConnectionId == connectionId ? "White" :
                              game.Player2ConnectionId == connectionId ? "Black" : null;

            if (playerColor == null || playerColor != game.CurrentPlayer ||
                !game.IsGameStarted || game.IsGameOver)
                return false;

            if (!IsMoveValid(game, move, playerColor))
                return false;

            ApplyMove(game, move, playerColor);

            game.CurrentPlayer = game.CurrentPlayer == "White" ? "Black" : "White";

            if (IsNoPiecesForOpponent(game, playerColor))
            {
                game.IsGameOver = true;
                game.Winner = playerColor;
            }

            updated = game;

            return true;
        }

        /// <summary>
        /// Создаёт стандартную начальную доску шашек 8x8.
        /// Шашки размещаются только на тёмных клетках: чёрные сверху (строки 0-2),
        /// белые снизу (строки 5-7). Верхний левый угол — тёмная клетка.
        /// </summary>
        private List<List<Cell>> CreateInitialBoard()
        {
            var board = new List<List<Cell>>();

            for (int row = 0; row < 8; row++)
            {
                var rowList = new List<Cell>();
                for (int col = 0; col < 8; col++)
                {
                    var cell = new Cell
                    {
                        Row = row,
                        Col = col,
                        PieceColor = "None",
                        IsKing = false
                    };

                    // Тёмные клетки для шашек
                    if ((row + col) % 2 == 0)
                    {
                        if (row <= 2)
                            cell.PieceColor = "Black";  // Чёрные сверху
                        else if (row >= 5)
                            cell.PieceColor = "White";  // Белые снизу
                    }

                    rowList.Add(cell);
                }
                board.Add(rowList);
            }

            return board;
        }

        /// <summary>
        /// Проверяет допустимость хода согласно правилам шашек.
        /// Поддерживает обычные ходы и взятия для простых и дамских шашек.
        /// </summary>
        private bool IsMoveValid(GameState game, MoveRequest move, string playerColor)
        {
            if (move.FromRow < 0 || move.FromRow > 7 || move.FromCol < 0 || move.FromCol > 7 ||
                move.ToRow < 0 || move.ToRow > 7 || move.ToCol < 0 || move.ToCol > 7)
                return false;

            var from = game.Board[move.FromRow][move.FromCol];
            var to = game.Board[move.ToRow][move.ToCol];

            if (from.PieceColor != playerColor || to.PieceColor != "None")
                return false;

            int rowDiff = Math.Abs(move.ToRow - move.FromRow);
            int colDiff = Math.Abs(move.ToCol - move.FromCol);

            // Обычный ход (1 клетка по диагонали)
            if (rowDiff == 1 && colDiff == 1)
            {
                if (from.IsKing) return true; // Дамка ходит во все стороны

                // Простая шашка только вперёд
                int dir = playerColor == "White" ? -1 : 1;
                return (move.ToRow - move.FromRow) == dir;
            }

            // Взятие (2 клетки через противника)
            if (rowDiff == 2 && colDiff == 2)
            {
                int midRow = (move.FromRow + move.ToRow) / 2;
                int midCol = (move.FromCol + move.ToCol) / 2;
                var midCell = game.Board[midRow][midCol];

                string opponent = playerColor == "White" ? "Black" : "White";

                return midCell.PieceColor == opponent;
            }

            return false;
        }

        /// <summary>
        /// Применяет допустимый ход к состоянию игры.
        /// Перемещает шашку, создаёт дамку при достижении края доски,
        /// удаляет взятую фигуру при нужде.
        /// </summary>
        private void ApplyMove(GameState game, MoveRequest move, string playerColor)
        {
            var from = game.Board[move.FromRow][move.FromCol];
            var to = game.Board[move.ToRow][move.ToCol];

            // Перемещаем шашку
            to.PieceColor = from.PieceColor;
            to.IsKing = from.IsKing;

            // Очищаем исходную клетку
            from.PieceColor = "None";
            from.IsKing = false;

            // Проверяем создание дамки
            if (playerColor == "White" && move.ToRow == 0)
                to.IsKing = true;
            else if (playerColor == "Black" && move.ToRow == 7)
                to.IsKing = true;

            // Удаляем взятую шашку
            int rowDiff = Math.Abs(move.ToRow - move.FromRow);

            if (rowDiff == 2)
            {
                int midRow = (move.FromRow + move.ToRow) / 2;
                int midCol = (move.FromCol + move.ToCol) / 2;
                var midCell = game.Board[midRow][midCol];
                midCell.PieceColor = "None";
                midCell.IsKing = false;
            }
        }

        /// <summary>
        /// Проверяет, остались ли у противника фигуры на доске.
        /// </summary>
        private bool IsNoPiecesForOpponent(GameState game, string currentPlayerColor)
        {
            var opponent = currentPlayerColor == "White" ? "Black" : "White";

            return !game.Board.SelectMany(row => row).Any(cell => cell.PieceColor == opponent);
        }
    }
}
