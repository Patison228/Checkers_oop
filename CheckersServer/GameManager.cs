using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CheckersModels.Models;

namespace CheckersServer
{
    public class GameManager
    {
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

        private bool IsMoveValid(GameState game, MoveRequest move, string playerColor)
        {
            // Границы доски
            if (move.FromRow < 0 || move.FromRow > 7 || move.FromCol < 0 || move.FromCol > 7 ||
                move.ToRow < 0 || move.ToRow > 7 || move.ToCol < 0 || move.ToCol > 7)
                return false;

            var from = game.Board[move.FromRow][move.FromCol];
            var to = game.Board[move.ToRow][move.ToCol];

            if (from.PieceColor != playerColor || to.PieceColor != "None")
                return false;

            int rowDiff = Math.Abs(move.ToRow - move.FromRow);
            int colDiff = Math.Abs(move.ToCol - move.FromCol);

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

        private void ApplyMove(GameState game, MoveRequest move, string playerColor)
        {
            var from = game.Board[move.FromRow][move.FromCol];
            var to = game.Board[move.ToRow][move.ToCol];

            to.PieceColor = from.PieceColor;
            to.IsKing = from.IsKing;

            from.PieceColor = "None";
            from.IsKing = false;

            if (playerColor == "White" && move.ToRow == 0)
                to.IsKing = true;
            else if (playerColor == "Black" && move.ToRow == 7)
                to.IsKing = true;

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

        private bool IsNoPiecesForOpponent(GameState game, string currentPlayerColor)
        {
            var opponent = currentPlayerColor == "White" ? "Black" : "White";
            return !game.Board.SelectMany(row => row).Any(c => c.PieceColor == opponent);
        }
    }
}
