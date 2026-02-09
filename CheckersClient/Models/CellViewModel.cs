using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace CheckersClient.Models
{
    /// <summary>
    /// ViewModel для одной клетки доски шашек.
    /// </summary>
    public class CellViewModel : INotifyPropertyChanged
    {
        private readonly int _row, _col;
        private bool _isKing;
        private string _pieceColor = "None";
        private bool _isSelected;
        private bool _isPossibleMove;
        private bool _isMandatoryCapture;

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
                OnPropertyChanged(nameof(BorderBrush));
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
                OnPropertyChanged(nameof(BorderBrush));
                OnPropertyChanged(nameof(BorderThickness));
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
                OnPropertyChanged(nameof(MoveIndicatorVisibility));
            }
        }

        /// <summary>
        /// Обязательное взятие - подсвечивается оранжевым.
        /// </summary>
        public bool IsMandatoryCapture
        {
            get => _isMandatoryCapture;
            set
            {
                _isMandatoryCapture = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(BorderBrush));
                OnPropertyChanged(nameof(CaptureIndicatorVisibility));
            }
        }

        /// <summary>
        /// Фон клетки с учётом всех состояний.
        /// </summary>
        public Brush Background
        {
            get
            {
                if (IsSelected)
                    return new SolidColorBrush(Color.FromRgb(255, 255, 150)); // Светло-жёлтый

                if (IsMandatoryCapture)
                    return new SolidColorBrush(Color.FromRgb(255, 200, 100)); // Оранжевый

                if (IsPossibleMove)
                    return new SolidColorBrush(Color.FromRgb(144, 238, 144)); // Светло-зелёный

                // Шахматная расцветка
                return (Row + Col) % 2 == 0
                    ? new SolidColorBrush(Color.FromRgb(139, 69, 19)) // Коричневый
                    : new SolidColorBrush(Color.FromRgb(245, 222, 179)); // Пшеничный
            }
        }

        /// <summary>
        /// Обводка для выделенной клетки.
        /// </summary>
        public Brush BorderBrush => IsSelected || IsMandatoryCapture
            ? Brushes.Gold
            : Brushes.Transparent;

        public Thickness BorderThickness => IsSelected || IsMandatoryCapture
            ? new Thickness(3)
            : new Thickness(0);

        public Brush PieceWpfColor => PieceColor switch
        {
            "White" => Brushes.WhiteSmoke,
            "Black" => new SolidColorBrush(Color.FromRgb(50, 50, 50)),
            _ => Brushes.Transparent
        };

        public Visibility PieceVisibility => PieceColor == "None" ? Visibility.Collapsed : Visibility.Visible;
        public Visibility KingIndicatorVisibility => IsKing ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MoveIndicatorVisibility => IsPossibleMove && !IsMandatoryCapture ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CaptureIndicatorVisibility => IsMandatoryCapture ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
