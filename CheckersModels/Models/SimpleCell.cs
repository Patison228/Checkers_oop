namespace CheckersModels.Models
{
    public class SimpleCell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string Color { get; set; } = "None";  
        public string Type { get; set; } = "Empty";  
    }
}