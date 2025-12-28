using System.Text.Json.Serialization;

namespace CheckersModels.Models
{
    public class Cell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PieceColor Color { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PieceType Type { get; set; }
    }
}
