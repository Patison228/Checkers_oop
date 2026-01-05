namespace CheckersModels.Models
{
    public class Cell
    {
        public int Row { get; set; }
        public int Col { get; set; }

        // "None", "White", "Black"
        public string PieceColor { get; set; } = "None";

        public bool IsKing { get; set; }
    }
}