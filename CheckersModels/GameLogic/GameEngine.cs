using CheckersModels.Models;

namespace CheckersModels.GameLogic
{
    public static class GameEngine
    {
        public static void InitializeBoard(GameState gameState)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var cell = new Cell { Row = row, Col = col };

                    if ((row + col) % 2 == 0)
                    {
                        cell.Color = PieceColor.None;
                        cell.Type = PieceType.Empty;
                    }
                    else
                    {
                        if (row < 3)
                        {
                            cell.Color = PieceColor.White;
                            cell.Type = PieceType.Man;
                        }
                        else if (row > 4)
                        {
                            cell.Color = PieceColor.Black;
                            cell.Type = PieceType.Man;
                        }
                        else
                        {
                            cell.Color = PieceColor.None;
                            cell.Type = PieceType.Empty;
                        }
                    }
                    gameState.Board[row, col] = cell;
                }
            }
        }

        public static bool IsValidMove(GameState gameState, Move move)
        {
            if (move.FromRow < 0 || move.FromRow >= 8 || move.FromCol < 0 || move.FromCol >= 8 ||
                move.ToRow < 0 || move.ToRow >= 8 || move.ToCol < 0 || move.ToCol >= 8)
                return false;

            var fromCell = gameState.Board[move.FromRow, move.FromCol];
            var toCell = gameState.Board[move.ToRow, move.ToCol];

            if (fromCell.Color != (gameState.CurrentPlayer == "White" ? PieceColor.White : PieceColor.Black))
                return false;

            if (toCell.Type != PieceType.Empty)
                return false;

            int rowDiff = Math.Abs(move.ToRow - move.FromRow);
            int colDiff = Math.Abs(move.ToCol - move.FromCol);

            return rowDiff == 1 && colDiff == 1;
        }

        public static void MakeMove(GameState gameState, Move move)
        {
            var fromCell = gameState.Board[move.FromRow, move.FromCol];
            gameState.Board[move.ToRow, move.ToCol] = fromCell;
            gameState.Board[move.FromRow, move.FromCol] = new Cell
            {
                Row = move.FromRow,
                Col = move.FromCol,
                Color = PieceColor.None,
                Type = PieceType.Empty
            };

            gameState.CurrentPlayer = gameState.CurrentPlayer == "White" ? "Black" : "White";
        }
    }
}
