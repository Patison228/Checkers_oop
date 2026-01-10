using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace CheckersClient.Models
{
    public class CellViewModel : INotifyPropertyChanged
    {
        private readonly int _row, _col;
        private string _pieceColor = "None";
        private bool _isKing;

        public CellViewModel(int row, int col)
        {
            _row = row;
            _col = col;
        }

        public int Row => _row;
        public int Col => _col;

        public string PieceColor
        {
            get => _pieceColor;
            set
            {
                _pieceColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PieceWpfColor));
                OnPropertyChanged(nameof(PieceVisibility));
            }
        }

        public bool IsKing
        {
            get => _isKing;
            set { _isKing = value; OnPropertyChanged(); }
        }

        // Шахматная доска
        public Brush Background => (Row + Col) % 2 == 0
            ? Brushes.SaddleBrown   // Тёмная клетка
            : Brushes.Ivory;        // Светлая клетка

        // Цвет шашки
        public Brush PieceWpfColor => PieceColor switch
        {
            "White" => Brushes.WhiteSmoke,
            "Black" => Brushes.DimGray,
            _ => Brushes.Transparent
        };

        // Видимость шашки
        public Visibility PieceVisibility => PieceColor == "None"
            ? Visibility.Collapsed
            : Visibility.Visible;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
