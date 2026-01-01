namespace CheckersModels.Models
{
    public class CellDto
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string Color { get; set; } = "None";   // "White","Black","None"
        public string Type { get; set; } = "Empty";   // "Man","King","Empty"
    }

    public class GameState
    {
        public List<List<CellDto>> Board { get; set; } = new();

        public string CurrentPlayer { get; set; } = "White";
        public string RoomId { get; set; } = "";
        public string Player1 { get; set; } = "";
        public string Player2 { get; set; } = "";
        public bool IsGameStarted { get; set; }
        public bool IsGameOver { get; set; }
        public string Winner { get; set; } = "";
    }
}
