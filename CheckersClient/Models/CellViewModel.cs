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
        private bool _isSelected;
        private bool _isPossibleMove;

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
                OnPropertyChanged(nameof(Background));
            }
        }

        public bool IsKing
        {
            get => _isKing;
            set
            {
                _isKing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KingIndicatorVisibility));
                OnPropertyChanged(nameof(Background));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Background));
            }
        }

        public bool IsPossibleMove
        {
            get => _isPossibleMove;
            set
            {
                _isPossibleMove = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Background));
            }
        }

        public Brush Background => IsSelected ? Brushes.LightYellow :
                                  IsPossibleMove ? Brushes.LightGreen :
                                  (Row + Col) % 2 == 0 ? Brushes.SaddleBrown : Brushes.Ivory;

        public Brush PieceWpfColor => PieceColor switch
        {
            "White" => Brushes.WhiteSmoke,
            "Black" => Brushes.DimGray,
            _ => Brushes.Transparent
        };

        public Visibility PieceVisibility => PieceColor == "None" ? Visibility.Collapsed : Visibility.Visible;

        public Visibility KingIndicatorVisibility => IsKing ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
