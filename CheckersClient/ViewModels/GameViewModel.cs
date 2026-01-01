using CheckersClient.Services;
using CheckersModels.GameLogic;
using CheckersModels.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.CompilerServices;

namespace CheckersClient.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private SignalRService _signalRService;
        private GameState _gameState = new GameState();
        private ObservableCollection<ObservableCollection<CellViewModel>> _board;
        private string _currentPlayer = "Ожидание...";
        private string _status = "Загрузка игры...";
        private int? _selectedRow;
        private int? _selectedCol;

        public ObservableCollection<ObservableCollection<CellViewModel>> Board
        {
            get => _board ??= new();
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
                _gameState = value ?? new GameState();
                UpdateBoardDisplay();
            }
        }

        public GameViewModel()
        {
            CellClickCommand = new RelayCommand(CellClick);
            InitializeEmptyBoard();
        }

        private void InitializeEmptyBoard()
        {
            Board.Clear();
            for (int row = 0; row < 8; row++)
            {
                var rowCells = new ObservableCollection<CellViewModel>();
                for (int col = 0; col < 8; col++)
                {
                    rowCells.Add(new CellViewModel(row, col));
                }
                Board.Add(rowCells);
            }
        }

        private async void CellClick(object parameter)
        {
            if (parameter is int index && GameState.Board.Count == 8)
            {
                int row = index / 8;
                int col = index % 8;
                await ProcessCellClick(row, col);
            }
        }

        private async Task ProcessCellClick(int row, int col)
        {
            try
            {
                var cell = GameState.Board[row][col];
                string currentColor = GameState.CurrentPlayer; // "White"/"Black"

                if (_selectedRow.HasValue && _selectedCol.HasValue)
                {
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
                        Status = "Ход отправлен";
                    }
                    else
                    {
                        Status = "Недопустимый ход";
                    }

                    _selectedRow = null;
                    _selectedCol = null;
                }
                else if (cell.Color == currentColor && cell.Type != "Empty")
                {
                    _selectedRow = row;
                    _selectedCol = col;
                }
                else
                {
                    Status = "Выберите свою шашку";
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
            if (GameState.Board == null || GameState.Board.Count != 8) return;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var dto = GameState.Board[row][col];
                    var vm = Board[row][col];

                    vm.PieceColor = dto.Color switch
                    {
                        "White" => PieceColor.White,
                        "Black" => PieceColor.Black,
                        _ => PieceColor.None
                    };

                    vm.Type = dto.Type switch
                    {
                        "Man" => PieceType.Man,
                        "King" => PieceType.King,
                        _ => PieceType.Empty
                    };

                    vm.IsSelected = _selectedRow == row && _selectedCol == col;
                }
            }

            CurrentPlayer = $"Ход: {GameState.CurrentPlayer}";
        }
    }

    public class CellViewModel : INotifyPropertyChanged
    {
        private readonly int _row;
        private readonly int _col;
        private PieceColor _pieceColor = PieceColor.None;
        private PieceType _type = PieceType.Empty;
        private bool _isSelected;

        public int Row => _row;
        public int Col => _col;

        public Brush Background
        {
            get
            {
                if (_isSelected) return Brushes.LightGreen;
                bool dark = (_row + _col) % 2 == 0;
                return dark ? Brushes.SaddleBrown : Brushes.BurlyWood;
            }
        }

        public Color PieceWpfColor
        {
            get
            {
                return _pieceColor switch
                {
                    PieceColor.White => Colors.White,
                    PieceColor.Black => Colors.Black,
                    _ => Colors.Transparent
                };
            }
        }

        public PieceColor PieceColor
        {
            get => _pieceColor;
            set
            {
                if (_pieceColor != value)
                {
                    _pieceColor = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PieceWpfColor));
                    OnPropertyChanged(nameof(Background));
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
                    OnPropertyChanged(nameof(Background));
                }
            }
        }

        public CellViewModel(int row, int col)
        {
            _row = row;
            _col = col;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
