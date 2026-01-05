using CheckersModels.Models;
using System.Collections.Generic;

namespace CheckersServer.Models
{
    // Состояние одной партии в комнате
    public class GameState
    {
        public string RoomId { get; set; } = string.Empty;
        public string Player1ConnectionId { get; set; } = string.Empty;
        public string Player2ConnectionId { get; set; } = string.Empty;

        // "White" или "Black"
        public string CurrentPlayer { get; set; } = "White";

        public bool IsGameStarted { get; set; }
        public bool IsGameOver { get; set; }
        public string Winner { get; set; } = string.Empty;

        // Доска 8x8 как список списков, удобный для JSON
        public List<List<Cell>> Board { get; set; } = new();
    }
}
