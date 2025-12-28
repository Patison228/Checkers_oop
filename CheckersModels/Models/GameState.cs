using System.Text.Json.Serialization;

namespace CheckersModels.Models
{
    public class GameState
    {
        public List<List<SimpleCell>> Board { get; set; } = new(); 
        public string CurrentPlayer { get; set; } = "White";
        public string RoomId { get; set; } = "";
        public string Player1 { get; set; } = "";
        public string Player2 { get; set; } = "";
        public bool IsGameStarted { get; set; }
        public bool IsGameOver { get; set; }
        public string Winner { get; set; } = "";
    }
}
