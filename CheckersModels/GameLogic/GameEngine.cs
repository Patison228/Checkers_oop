using CheckersModels.Models;

namespace CheckersModels.GameLogic
{
    public static class GameEngine
    {
        public static void InitializeBoard(GameState gameState)
        {
            gameState.Board = new List<List<SimpleCell>>();

            for (int row = 0; row < 8; row++)
            {
                var rowCells = new List<SimpleCell>();
                for (int col = 0; col < 8; col++)
                {
                    var cell = new SimpleCell { Row = row, Col = col };

                    if ((row + col) % 2 == 0)
                    {
                        cell.Color = "None";
                        cell.Type = "Empty";
                    }
                    else
                    {
                        if (row < 3)
                        {
                            cell.Color = "White";
                            cell.Type = "Man";
                        }
                        else if (row > 4)
                        {
                            cell.Color = "Black";
                            cell.Type = "Man";
                        }
                        else
                        {
                            cell.Color = "None";
                            cell.Type = "Empty";
                        }
                    }
                    rowCells.Add(cell);
                }
                gameState.Board.Add(rowCells);
            }
        }

        public static bool IsValidMove(GameState gameState, Move move)
        {
            if (move.FromRow < 0 || move.FromRow >= 8 || move.FromCol < 0 || move.FromCol >= 8 ||
                move.ToRow < 0 || move.ToRow >= 8 || move.ToCol < 0 || move.ToCol >= 8)
                return false;

            var fromCell = gameState.Board[move.FromRow][move.FromCol];
            var toCell = gameState.Board[move.ToRow][move.ToCol];

            PieceColor fromColor = fromCell.Color == "White" ? PieceColor.White :
                                 fromCell.Color == "Black" ? PieceColor.Black : PieceColor.None;

            if (fromColor != (gameState.CurrentPlayer == "White" ? PieceColor.White : PieceColor.Black))
                return false;

            if (toCell.Type != "Empty")
                return false;

            int rowDiff = Math.Abs(move.ToRow - move.FromRow);
            int colDiff = Math.Abs(move.ToCol - move.FromCol);
            return rowDiff == 1 && colDiff == 1;
        }

        public static void MakeMove(GameState gameState, Move move)
        {
            var fromCell = gameState.Board[move.FromRow][move.FromCol];
            gameState.Board[move.ToRow][move.ToCol] = fromCell;

            gameState.Board[move.FromRow][move.FromCol] = new SimpleCell
            {
                Row = move.FromRow,
                Col = move.FromCol,
                Color = "None",
                Type = "Empty"
            };

            gameState.CurrentPlayer = gameState.CurrentPlayer == "White" ? "Black" : "White";
        }
    }
}
