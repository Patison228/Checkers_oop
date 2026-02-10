using System.Collections.Concurrent;
using CheckersModels.Models;

namespace CheckersServer
{
    /// <summary>
    /// Менеджер состояния всех активных игр на сервере.
    /// Отвечает за создание комнат, подключение игроков и валидацию ходов.
    /// </summary>
    public class GameManager
    {
        private readonly ConcurrentDictionary<string, GameState> _games = new();

        /// <summary>
        /// Создаёт новую комнату для одного игрока.
        /// </summary>
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
                Board = CreateInitialBoard(),
                MustContinueCapture = false,
                ContinueCaptureFromRow = -1,
                ContinueCaptureFromCol = -1
            };

            _games[roomId] = game;
            return game;
        }

        /// <summary>
        /// Подключает второго игрока к комнате.
        /// </summary>
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

        public GameState? GetGame(string roomId)
        {
            _games.TryGetValue(roomId, out var game);
            return game;
        }

        /// <summary>
        /// Проверяет и применяет ход с учётом обязательности взятия и множественного рубания.
        /// </summary>
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

            if (game.MustContinueCapture)
            {
                if (move.FromRow != game.ContinueCaptureFromRow || move.FromCol != game.ContinueCaptureFromCol)
                    return false;
            }

            if (!IsMoveValid(game, move, playerColor))
                return false;

            bool isCapture = Math.Abs(move.ToRow - move.FromRow) == 2;

            ApplyMove(game, move, playerColor);

            if (isCapture)
            {
                var furtherCaptures = GetPossibleCaptures(game, move.ToRow, move.ToCol, playerColor);
                if (furtherCaptures.Count > 0)
                {
                    game.MustContinueCapture = true;
                    game.ContinueCaptureFromRow = move.ToRow;
                    game.ContinueCaptureFromCol = move.ToCol;
                    updated = game;
                    return true;
                }
            }

            game.MustContinueCapture = false;
            game.ContinueCaptureFromRow = -1;
            game.ContinueCaptureFromCol = -1;
            game.CurrentPlayer = game.CurrentPlayer == "White" ? "Black" : "White";

            if (IsNoPiecesForOpponent(game, playerColor))
            {
                game.IsGameOver = true;
                game.Winner = playerColor;
            }
            else if (!HasAnyMoves(game, game.CurrentPlayer))
            {
                game.IsGameOver = true;
                game.Winner = playerColor;
            }

            updated = game;
            return true;
        }

        /// <summary>
        /// Создаёт стандартную начальную доску шашек 8x8.
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

                    if ((row + col) % 2 == 0)
                    {
                        if (row <= 2)
                            cell.PieceColor = "Black";
                        else if (row >= 5)
                            cell.PieceColor = "White";
                    }

                    rowList.Add(cell);
                }
                board.Add(rowList);
            }

            return board;
        }

        /// <summary>
        /// Проверяет допустимость хода с учётом обязательности взятия.
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

            if (!game.MustContinueCapture)
            {
                var allCaptures = GetAllPossibleCaptures(game, playerColor);
                if (allCaptures.Count > 0)
                {
                    if (rowDiff != 2 || colDiff != 2)
                        return false;

                    bool isValidCapture = allCaptures.Any(c =>
                        c.FromRow == move.FromRow && c.FromCol == move.FromCol &&
                        c.ToRow == move.ToRow && c.ToCol == move.ToCol);

                    if (!isValidCapture)
                        return false;

                    return true;
                }
            }

            if (rowDiff == 1 && colDiff == 1)
            {
                if (from.IsKing) return true;

                int dir = playerColor == "White" ? -1 : 1;
                return (move.ToRow - move.FromRow) == dir;
            }

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
        /// </summary>
        private void ApplyMove(GameState game, MoveRequest move, string playerColor)
        {
            var from = game.Board[move.FromRow][move.FromCol];
            var to = game.Board[move.ToRow][move.ToCol];

            to.PieceColor = from.PieceColor;
            to.IsKing = from.IsKing;

            from.PieceColor = "None";
            from.IsKing = false;

            // Превращение в дамку
            if (playerColor == "White" && move.ToRow == 0)
                to.IsKing = true;
            else if (playerColor == "Black" && move.ToRow == 7)
                to.IsKing = true;

            // Удаление взятой шашки
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
        /// Возвращает все возможные взятия для указанной шашки.
        /// </summary>
        private List<MoveRequest> GetPossibleCaptures(GameState game, int row, int col, string playerColor)
        {
            var captures = new List<MoveRequest>();
            var cell = game.Board[row][col];

            if (cell.PieceColor != playerColor)
                return captures;

            string opponent = playerColor == "White" ? "Black" : "White";


            List<(int rowDir, int colDir)> directions = new()
            {
                (-1, -1), 
                (-1, 1),  
                (1, -1),  
                (1, 1)    
            };

            foreach (var (rowDir, colDir) in directions)
            {
                int jumpRow = row + rowDir * 2;
                int jumpCol = col + colDir * 2;

                if (jumpRow >= 0 && jumpRow < 8 && jumpCol >= 0 && jumpCol < 8)
                {
                    int midRow = row + rowDir;
                    int midCol = col + colDir;

                    var midCell = game.Board[midRow][midCol];
                    var targetCell = game.Board[jumpRow][jumpCol];

                    if (midCell.PieceColor == opponent && targetCell.PieceColor == "None")
                    {
                        captures.Add(new MoveRequest
                        {
                            RoomId = game.RoomId,
                            FromRow = row,
                            FromCol = col,
                            ToRow = jumpRow,
                            ToCol = jumpCol
                        });
                    }
                }
            }

            return captures;
        }

        /// <summary>
        /// Возвращает все возможные взятия для игрока.
        /// </summary>
        private List<MoveRequest> GetAllPossibleCaptures(GameState game, string playerColor)
        {
            var allCaptures = new List<MoveRequest>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (game.Board[row][col].PieceColor == playerColor)
                    {
                        var captures = GetPossibleCaptures(game, row, col, playerColor);
                        allCaptures.AddRange(captures);
                    }
                }
            }

            return allCaptures;
        }

        /// <summary>
        /// Проверяет наличие фигур противника.
        /// </summary>
        private bool IsNoPiecesForOpponent(GameState game, string currentPlayerColor)
        {
            var opponent = currentPlayerColor == "White" ? "Black" : "White";
            return !game.Board.SelectMany(row => row).Any(cell => cell.PieceColor == opponent);
        }

        /// <summary>
        /// Проверяет, есть ли доступные ходы у игрока.
        /// </summary>
        private bool HasAnyMoves(GameState game, string playerColor)
        {
            var captures = GetAllPossibleCaptures(game, playerColor);
            if (captures.Count > 0)
                return true;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var cell = game.Board[row][col];
                    if (cell.PieceColor == playerColor)
                    {
                        int[] rowDirs = cell.IsKing ? new[] { -1, 1 } : new[] { playerColor == "White" ? -1 : 1 };
                        int[] colDirs = { -1, 1 };

                        foreach (int rowDir in rowDirs)
                        {
                            foreach (int colDir in colDirs)
                            {
                                int newRow = row + rowDir;
                                int newCol = col + colDir;

                                if (newRow >= 0 && newRow < 8 && newCol >= 0 && newCol < 8)
                                {
                                    if (game.Board[newRow][newCol].PieceColor == "None")
                                        return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
