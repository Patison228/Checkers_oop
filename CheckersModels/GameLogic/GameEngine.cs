using CheckersModels.Models;

namespace CheckersModels.GameLogic
{
    public static class GameEngine
    {
        public static void InitializeBoard(GameState gameState)
        {
            gameState.Board = new List<List<CellDto>>();

            for (int row = 0; row < 8; row++)
            {
                var rowCells = new List<CellDto>();
                for (int col = 0; col < 8; col++)
                {
                    var cell = new CellDto { Row = row, Col = col };

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
            if (gameState.Board.Count != 8) return false;

            if (move.FromRow < 0 || move.FromRow >= 8 ||
                move.FromCol < 0 || move.FromCol >= 8 ||
                move.ToRow < 0 || move.ToRow >= 8 ||
                move.ToCol < 0 || move.ToCol >= 8)
                return false;

            var fromCell = gameState.Board[move.FromRow][move.FromCol];
            var toCell = gameState.Board[move.ToRow][move.ToCol];

            if (toCell.Type != "Empty") return false;

            string currentColor = gameState.CurrentPlayer; // "White" или "Black"
            if (fromCell.Color != currentColor) return false;

            int rowDiff = move.ToRow - move.FromRow;
            int colDiff = Math.Abs(move.ToCol - move.FromCol);

            // Простейшая проверка: один шаг по диагонали
            if (Math.Abs(rowDiff) != 1 || colDiff != 1)
                return false;

            // Доп. ограничение направления для простых шашек:
            if (fromCell.Type == "Man")
            {
                if (fromCell.Color == "White" && rowDiff != 1) return false;
                if (fromCell.Color == "Black" && rowDiff != -1) return false;
            }

            return true;
        }

        public static void MakeMove(GameState gameState, Move move)
        {
            var fromCell = gameState.Board[move.FromRow][move.FromCol];
            var toCell = gameState.Board[move.ToRow][move.ToCol];

            // Переносим данные клетки
            toCell.Color = fromCell.Color;
            toCell.Type = fromCell.Type;

            // Очищаем старую
            fromCell.Color = "None";
            fromCell.Type = "Empty";

            // Смена игрока
            gameState.CurrentPlayer = gameState.CurrentPlayer == "White" ? "Black" : "White";
        }
    }
}
