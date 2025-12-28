using CheckersClient.Services;
using CheckersModels.GameLogic;
using CheckersModels.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace CheckersClient.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private SignalRService _signalRService;
        private GameState _gameState;
        private ObservableCollection<ObservableCollection<CellViewModel>> _board;
        private string _currentPlayer = "Подключение...";
        private int? _selectedRow;
        private int? _selectedCol;
        private string _roomId;
        private string _status = "Ожидание данных...";

        public ObservableCollection<ObservableCollection<CellViewModel>> Board
        {
            get => _board;
            set => SetProperty(ref _board, value);
        }

        public string CurrentPlayer
        {
            get => _currentPlayer;
            set => SetProperty(ref _currentPlayer, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string RoomId
        {
            get => _roomId;
            set => SetProperty(ref _roomId, value);
        }

        public ICommand CellClickCommand { get; }

        public SignalRService SignalRService
        {
            get => _signalRService;
            set => SetProperty(ref _signalRService, value);
        }

        public GameState GameState
        {
            get => _gameState;
            set
            {
                if (SetProperty(ref _gameState, value))
                {
                    RoomId = value.RoomId;
                    UpdateBoardDisplay();
                }
            }
        }

        public GameViewModel()
        {
            CellClickCommand = new RelayCommand(CellClick);
            Board = new ObservableCollection<ObservableCollection<CellViewModel>>();

            // Инициализация пустой доски
            InitializeEmptyBoard();
        }

        private void InitializeEmptyBoard()
        {
            Board.Clear();
            for (int row = 0; row < 8; row++)
            {
                var rowCollection = new ObservableCollection<CellViewModel>();
                for (int col = 0; col < 8; col++)
                {
                    rowCollection.Add(new CellViewModel(row, col));
                }
                Board.Add(rowCollection);
            }
        }

        private async void CellClick(object parameter)
        {
            if (parameter == null || GameState == null) return;

            // Получаем координаты из Button Tag или используем упрощенную логику
            if (int.TryParse(parameter.ToString(), out int row))
            {
                int col = row % 8;
                row /= 8;

                await ProcessCellClick(row, col);
            }
        }

        private async Task ProcessCellClick(int row, int col)
        {
            try
            {
                Status = $"Клик: {row},{col}";

                // Проверяем, можем ли ходить (текущий игрок)
                var cell = GameState.Board[row, col];
                PieceColor currentPlayerColor = GameState.CurrentPlayer == "White" ?
                    PieceColor.White : PieceColor.Black;

                if (_selectedRow.HasValue && _selectedCol.HasValue)
                {
                    // Пытаемся сделать ход
                    var move = new Move
                    {
                        FromRow = _selectedRow.Value,
                        FromCol = _selectedCol.Value,
                        ToRow = row,
                        ToCol = col
                    };

                    if (GameEngine.IsValidMove(GameState, move))
                    {
                        await SignalRService.MakeMoveAsync(GameState.RoomId, move);
                        Status = "Ход отправлен!";
                    }
                    else
                    {
                        Status = "Недопустимый ход!";
                    }

                    _selectedRow = null;
                    _selectedCol = null;
                }
                else if (cell.Color == currentPlayerColor && cell.Type != PieceType.Empty)
                {
                    // Выбираем шашку
                    _selectedRow = row;
                    _selectedCol = col;
                    Status = $"Выбрана шашка: {row},{col}";
                }
                else
                {
                    Status = "Выберите свою шашку!";
                }

                UpdateBoardDisplay();
            }
            catch (Exception ex)
            {
                Status = $"Ошибка: {ex.Message}";
            }
        }

        public void UpdateBoardDisplay()
        {
            if (GameState?.Board == null) return;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var gameCell = GameState.Board[row, col];
                    var viewCell = Board[row][col];

                    viewCell.Color = gameCell.Color;
                    viewCell.Type = gameCell.Type;
                    viewCell.IsSelected = row == _selectedRow && col == _selectedCol;
                    viewCell.Row = row;
                    viewCell.Col = col;
                }
            }

            CurrentPlayer = $"Ход: {GameState.CurrentPlayer} | Комната: {GameState.RoomId}";
            Status = $"Игрок 1: {GameState.Player1} | Игрок 2: {GameState.Player2}";
        }
    }

    // Вспомогательный класс для отображения ячеек (замена ObservableCell)
    public class CellViewModel : INotifyPropertyChanged
    {
        public int Row { get; set; }
        public int Col { get; set; }

        private PieceColor _color = PieceColor.None;
        private PieceType _type = PieceType.Empty;
        private bool _isSelected;

        public PieceColor Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public PieceType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public class CellViewModel : INotifyPropertyChanged
        {
            private PieceColor _color = PieceColor.None;
            private PieceType _type = PieceType.Empty;
            private bool _isSelected;

            public int Row { get; set; }
            public int Col { get; set; }

            // Фон ячейки (шахматная доска + подсветка)
            public Brush Background
            {
                get
                {
                    if (IsSelected)
                        return Brushes.LightGreen; // Зеленая подсветка выбранной

                    // Обычные цвета доски
                    bool isDarkSquare = (Row + Col) % 2 == 0;
                    return isDarkSquare ?
                        Brushes.SaddleBrown :  // Темно-коричневый
                        Brushes.BurlyWood;     // Бежевый
                }
            }

            // Цвет шашки
            public System.Windows.Media.Color PieceColor
            {
                get
                {
                    return Color switch
                    {
                        PieceColor.White => System.Windows.Media.Colors.White,
                        PieceColor.Black => System.Windows.Media.Colors.Black,
                        _ => System.Windows.Media.Colors.Transparent
                    };
                }
            }

            public PieceColor Color
            {
                get => _color;
                set
                {
                    if (_color != value)
                    {
                        _color = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(Background));  // Обновляем фон
                        OnPropertyChanged(nameof(PieceColor));   // Обновляем цвет шашки
                    }
                }
            }

            public PieceType Type
            {
                get => _type;
                set
                {
                    if (_type != value)
                    {
                        _type = value;
                        OnPropertyChanged();
                    }
                }
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(Background));  // Обновляем фон
                    }
                }
            }

            public CellViewModel(int row, int col)
            {
                Row = row;
                Col = col;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
