namespace CheckersModels.Models
{
    // Запрос хода от клиента
    public class MoveRequest
    {
        public string RoomId { get; set; } = string.Empty;
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
    }
}
