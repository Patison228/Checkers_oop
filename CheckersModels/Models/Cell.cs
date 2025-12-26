namespace CheckersModels.Models
{
    public class Cell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public PieceColor Color { get; set; }
        public PieceType Type { get; set; }
    }
}
