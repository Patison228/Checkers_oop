using CheckersClient.Models;
using CheckersModels.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CheckersClient.Converters
{
    [ValueConversion(typeof(PieceColor), typeof(Brush))]
    public class CellColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCell cell)
            {
                if (cell.IsSelected)
                    return Brushes.Yellow;

                return cell.Color switch
                {
                    PieceColor.White => Brushes.White,
                    PieceColor.Black => Brushes.Black,
                    PieceColor.None => (cell.Row + cell.Col) % 2 == 0 ? Brushes.Brown : Brushes.Beige,
                    _ => Brushes.Transparent
                };
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
