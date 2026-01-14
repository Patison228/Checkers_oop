using CheckersModels.Models;
using System.Collections.Generic;

namespace CheckersModels.Models
{
    // Состояние одной партии в комнате
    public class GameState
    {
        public string RoomId { get; set; } = string.Empty;
        public string Player1ConnectionId { get; set; } = string.Empty;
        public string Player2ConnectionId { get; set; } = string.Empty;
        public string CurrentPlayer { get; set; } = "White";
        public bool IsGameStarted { get; set; }
        public bool IsGameOver { get; set; }
        public string Winner { get; set; } = string.Empty;

        public List<List<Cell>> Board { get; set; } = new();
    }
}
