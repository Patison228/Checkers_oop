using System.Collections.Generic;

namespace CheckersModels.Models
{
    /// <summary>
    /// Состояние одной партии в комнате.
    /// </summary>
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

        /// <summary>
        /// Флаг обязательного продолжения взятия (множественное рубание).
        /// </summary>
        public bool MustContinueCapture { get; set; }

        /// <summary>
        /// Координаты шашки, которая должна продолжить взятие.
        /// </summary>
        public int ContinueCaptureFromRow { get; set; } = -1;
        public int ContinueCaptureFromCol { get; set; } = -1;
    }
}
