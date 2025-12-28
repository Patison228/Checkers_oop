using CheckersClient.Services;
using CheckersModels.GameLogic;
using CheckersModels.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
            if (parameter is int rowIndex && GameState.Board != null && GameState.Board.Count > 0)
            {
                int row = rowIndex / 8;
                int col = rowIndex % 8;

                await ProcessCellClick(row, col);
            }
        }

        private async Task ProcessCellClick(int row, int col)
        {
            try
            {
                if (GameState.Board.Count == 0 || row >= GameState.Board.Count ||
                    col >= GameState.Board[row].Count)
                    return;

                var cell = GameState.Board[row][col];
                string currentColor = GameState.CurrentPlayer == "White" ? "White" : "Black";

                Status = $"Клик: {row},{col}";

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
                        Status = "✅ Ход отправлен!";
                        await SignalRService.MakeMoveAsync(GameState.RoomId, move);
                    }
                    else
                    {
                        Status = "❌ Неверный ход!";
                    }

                    _selectedRow = null;
                    _selectedCol = null;
                }
                else if (cell.Color == currentColor && cell.Type != "Empty")
                {
                    _selectedRow = row;
                    _selectedCol = col;
                    Status = $"✅ Выбрано: {row},{col}";
                }
                else
                {
                    Status = "👆 Выберите свою шашку!";
                }

                UpdateBoardDisplay();
            }
            catch (Exception ex)
            {
                Status = $"❌ {ex.Message}";
            }
        }

        private void UpdateBoardDisplay()
        {
            try
            {
                if (GameState?.Board == null || GameState.Board.Count == 0) return;

                for (int row = 0; row < Math.Min(8, GameState.Board.Count); row++)
                {
                    if (row >= Board.Count) break;

                    for (int col = 0; col < Math.Min(8, GameState.Board[row].Count); col++)
                    {
                        if (col >= Board[row].Count) break;

                        var viewCell = Board[row][col];
                        var gameCell = GameState.Board[row][col];

                        viewCell.PieceColor = gameCell.Color switch
                        {
                            "White" => PieceColor.White,
                            "Black" => PieceColor.Black,
                            _ => PieceColor.None
                        };

                        viewCell.Type = gameCell.Type switch
                        {
                            "Man" => PieceType.Man,
                            "King" => PieceType.King,
                            _ => PieceType.Empty
                        };

                        viewCell.IsSelected = row == _selectedRow && col == _selectedCol;
                    }
                }

                CurrentPlayer = $"Ход: {GameState.CurrentPlayer}";
                Status = $"Комната: {GameState.RoomId}";
            }
            catch (Exception ex)
            {
                Status = $"UI: {ex.Message}";
            }
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
                bool isDarkSquare = (_row + _col) % 2 == 0;
                return isDarkSquare ? Brushes.SaddleBrown : Brushes.BurlyWood;
            }
        }

        public Color PieceWpfColor
        {
            get
            {
                switch (_pieceColor)
                {
                    case PieceColor.White: return Colors.White;
                    case PieceColor.Black: return Colors.Black;
                    default: return Colors.Transparent;
                }
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
                    OnPropertyChanged(nameof(Background));
                    OnPropertyChanged(nameof(PieceWpfColor));
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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
