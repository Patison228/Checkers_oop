using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CheckersModels.Models;

namespace CheckersServer
{
    public class GameManager
    {
        // RoomId -> GameState
        private readonly ConcurrentDictionary<string, GameState> _games = new();

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

        public GameState? JoinRoom(string roomId, string connectionId)
        {
            if (!_games.TryGetValue(roomId, out var game))
                return null;

            if (!string.IsNullOrEmpty(game.Player2ConnectionId))
                return null; // уже занято

            game.Player2ConnectionId = connectionId;
            game.IsGameStarted = true;
            return game;
        }

        public GameState? GetGame(string roomId)
        {
            _games.TryGetValue(roomId, out var game);
            return game;
        }

        public bool TryApplyMove(string connectionId, MoveRequest move, out GameState? updated)
        {
            updated = null;

            if (!_games.TryGetValue(move.RoomId, out var game))
                return false;

            // Проверка чей ход
            var playerColor = game.Player1ConnectionId == connectionId ? "White" :
                              game.Player2ConnectionId == connectionId ? "Black" : null;

            if (playerColor == null || playerColor != game.CurrentPlayer || !game.IsGameStarted || game.IsGameOver)
                return false;

            // Минимальная валидация хода (пример, без всех правил шашек)
            if (!IsMoveValid(game, move, playerColor))
                return false;

            ApplyMove(game, move, playerColor);

            // Смена хода
            game.CurrentPlayer = game.CurrentPlayer == "White" ? "Black" : "White";

            // Простейшая проверка конца игры (нет фигур у соперника)
            if (IsNoPiecesForOpponent(game, playerColor))
            {
                game.IsGameOver = true;
                game.Winner = playerColor;
            }

            updated = game;
            return true;
        }

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

                    // Стандартная расстановка: белые сверху, черные снизу
                    if ((row + col) % 2 == 1) // только темные клетки
                    {
                        if (row < 3)
                            cell.PieceColor = "Black";
                        else if (row > 4)
                            cell.PieceColor = "White";
                    }

                    rowList.Add(cell);
                }
                board.Add(rowList);
            }

            return board;
        }

        private bool IsMoveValid(GameState game, MoveRequest move, string playerColor)
        {
            // Базовая проверка диапазона
            if (move.FromRow < 0 || move.FromRow > 7 ||
                move.FromCol < 0 || move.FromCol > 7 ||
                move.ToRow < 0 || move.ToRow > 7 ||
                move.ToCol < 0 || move.ToCol > 7)
                return false;

            var from = game.Board[move.FromRow][move.FromCol];
            var to = game.Board[move.ToRow][move.ToCol];

            if (from.PieceColor != playerColor)
                return false;

            if (to.PieceColor != "None")
                return false;

            int rowDiff = move.ToRow - move.FromRow;
            int colDiff = Math.Abs(move.ToCol - move.FromCol);

            // Простое правило: ход на одну диагональ вперед (без взятий и дамок, для минимального варианта)
            if (colDiff != 1)
                return false;

            if (playerColor == "White" && rowDiff != -1 && !from.IsKing)
                return false;

            if (playerColor == "Black" && rowDiff != 1 && !from.IsKing)
                return false;

            // Для простоты здесь нет логики взятий, её можно добавить позже
            return true;
        }

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
            if (playerColor == "Black" && move.ToRow == 7)
                to.IsKing = true;
        }

        private bool IsNoPiecesForOpponent(GameState game, string currentPlayerColor)
        {
            var opponent = currentPlayerColor == "White" ? "Black" : "White";

            foreach (var row in game.Board)
            {
                foreach (var cell in row)
                {
                    if (cell.PieceColor == opponent)
                        return false;
                }
            }

            return true;
        }
    }
}
