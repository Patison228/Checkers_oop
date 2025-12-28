using CheckersModels.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CheckersClient.Models
{
    public class ObservableCell : INotifyPropertyChanged
    {
        private PieceColor _color;
        private PieceType _type;
        private bool _isSelected;

        public int Row { get; set; }
        public int Col { get; set; }

        public PieceColor Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }

        public PieceType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
