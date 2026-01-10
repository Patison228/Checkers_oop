using CheckersModels.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CheckersClient.Models
{
    public class CellViewModel : INotifyPropertyChanged
    {
        private string _pieceColor = "None";
        private bool _isKing;
        private bool _isSelected;
        private readonly int _row, _col;

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
                OnPropertyChanged(nameof(Background));
            }
        }

        public bool IsKing
        {
            get => _isKing;
            set { _isKing = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(Background)); }
        }

        public Brush Background => (Row + Col) % 2 == 0
            ? IsSelected ? Brushes.LightYellow : Brushes.Brown
            : IsSelected ? Brushes.LightYellow : Brushes.Beige;

        public Brush PieceWpfColor => PieceColor switch
        {
            "White" => Brushes.White,
            "Black" => Brushes.Black,
            _ => Brushes.Transparent
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
