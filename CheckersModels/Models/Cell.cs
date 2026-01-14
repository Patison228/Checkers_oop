namespace CheckersModels.Models
{
    /// <summary>
    /// Модель одной клетки игровой доски шашек 8x8.
    /// Содержит координаты клетки и информацию о шашке (если есть).
    /// Сериализуется для передачи по SignalR между клиентом и сервером.
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// Индекс строки на доске (0-7).
        /// Строка 0 — верхний край доски, строка 7 — нижний.
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Индекс столбца на доске (0-7).
        /// Столбец 0 — левый край доски, столбец 7 — правый.
        /// </summary>
        public int Col { get; set; }

        /// <summary>
        /// Цвет шашки на клетке или <c>"None"</c>, если клетка пустая.
        /// Возможные значения: <c>"None"</c>, <c>"White"</c>, <c>"Black"</c>.
        /// </summary>
        public string PieceColor { get; set; } = "None";

        /// <summary>
        /// Признак дамки. <c>true</c> если на клетке стоит дамка, <c>false</c> для обычной шашки.
        /// Имеет смысл только когда <see cref="PieceColor"/> не равен <c>"None"</c>.
        /// </summary>
        public bool IsKing { get; set; }
    }
}
