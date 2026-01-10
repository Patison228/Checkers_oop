using CheckersClient.Models;
using CheckersClient.Services;
using CheckersModels.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace CheckersClient.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private GameState _gameState;
        private ObservableCollection<ObservableCollection<CellViewModel>> _board;
        private CellViewModel? _selectedCell;
        private string _status = "Выберите свою шашку";
        private string _currentPlayer = "";

        public ObservableCollection<ObservableCollection<CellViewModel>> Board
        {
            get => _board;
            set { _board = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string CurrentPlayer => _currentPlayer;

        public ICommand CellClicked { get; }

        public GameViewModel(SignalRService signalRService, GameState initialState)
        {
            _signalRService = signalRService;
            _gameState = initialState;
            _signalRService.StateUpdated += OnStateUpdated;
            _signalRService.GameOver += OnGameOver;
            _signalRService.MoveRejected += OnMoveRejected;

            UpdateBoard(_gameState);
            CellClicked = new RelayCommand(OnCellClicked);
        }

        private void OnCellClicked(object? parameter)
        {
            if (parameter is not CellViewModel cell) return;

            // Если ничего не выбрано — выделяем шашку
            if (_selectedCell == null)
            {
                if (cell.PieceColor == _gameState.CurrentPlayer)
                {
                    SelectPiece(cell);
                }
                return;
            }

            // Если выбрана шашка — делаем ход
            if (IsValidMove(_selectedCell, cell))
            {
                MakeMove(_selectedCell, cell);
            }
            else
            {
                ClearSelection();
                Status = "Недопустимый ход. Выберите шашку.";
            }
        }

        private void SelectPiece(CellViewModel cell)
        {
            ClearSelection(); // Снимаем предыдущую
            _selectedCell = cell;
            cell.IsSelected = true;

            HighlightPossibleMoves(cell);
            Status = "Выберите клетку для хода";
        }

        private void ClearSelection()
        {
            if (_selectedCell != null)
            {
                _selectedCell.IsSelected = false;
                ClearHighlights();
                _selectedCell = null;
            }
        }

        private void ClearHighlights()
        {
            foreach (var row in Board)
                foreach (var cell in row)
                    cell.IsPossibleMove = false;
        }

        private void HighlightPossibleMoves(CellViewModel selected)
        {
            ClearHighlights();

            string playerColor = _gameState.CurrentPlayer;
            int rowDir = playerColor == "White" ? -1 : 1;

            // Обычные ходы (1 клетка)
            CheckMove(selected, rowDir, 1);
            CheckMove(selected, rowDir, -1);

            // Простые взятия (2 клетки, опционально)
            CheckCapture(selected, rowDir * 2, 2);
            CheckCapture(selected, rowDir * 2, -2);
        }

        private void CheckMove(CellViewModel from, int rowDelta, int colDelta)
        {
            int toRow = from.Row + rowDelta;
            int toCol = from.Col + colDelta;

            if (toRow >= 0 && toRow < 8 && toCol >= 0 && toCol < 8)
            {
                var toCell = GetCell(toRow, toCol);
                if (toCell.PieceColor == "None")
                    toCell.IsPossibleMove = true;
            }
        }

        private void CheckCapture(CellViewModel from, int rowDelta, int colDelta)
        {
            int toRow = from.Row + rowDelta;
            int toCol = from.Col + colDelta;

            if (toRow >= 0 && toRow < 8 && toCol >= 0 && toCol < 8)
            {
                var toCell = GetCell(toRow, toCol);
                if (toCell.PieceColor == "None")
                {
                    int midRow = (from.Row + toRow) / 2;
                    int midCol = (from.Col + toCol) / 2;
                    var midCell = GetCell(midRow, midCol);

                    string opponent = _gameState.CurrentPlayer == "White" ? "Black" : "White";
                    if (midCell.PieceColor == opponent)
                        toCell.IsPossibleMove = true;
                }
            }
        }

        private CellViewModel GetCell(int row, int col)
        {
            return Board[row][col];
        }

        private bool IsValidMove(CellViewModel from, CellViewModel to)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Col - from.Col);

            // Обычный ход
            if (rowDiff == 1 && colDiff == 1)
            {
                int expectedRowDir = _gameState.CurrentPlayer == "White" ? -1 : 1;
                return (to.Row - from.Row) == expectedRowDir || from.IsKing;
            }

            // Взятие
            if (rowDiff == 2 && colDiff == 2)
            {
                int midRow = (from.Row + to.Row) / 2;
                int midCol = (from.Col + to.Col) / 2;
                var midCell = GetCell(midRow, midCol);

                string opponent = _gameState.CurrentPlayer == "White" ? "Black" : "White";
                return midCell.PieceColor == opponent;
            }

            return false;
        }

        private async void MakeMove(CellViewModel from, CellViewModel to)
        {
            Status = "Отправляю ход...";
            ClearSelection();

            var move = new MoveRequest
            {
                RoomId = _gameState.RoomId,
                FromRow = from.Row,
                FromCol = from.Col,
                ToRow = to.Row,
                ToCol = to.Col
            };

            try
            {
                await _signalRService.SendMakeMove(move);
            }
            catch (Exception ex)
            {
                Status = $"Ошибка: {ex.Message}";
            }
        }

        private void UpdateBoard(GameState state)
        {
            _gameState = state;
            _currentPlayer = state.CurrentPlayer;

            Board = new ObservableCollection<ObservableCollection<CellViewModel>>();
            for (int r = 0; r < 8; r++)
            {
                var row = new ObservableCollection<CellViewModel>();
                for (int c = 0; c < 8; c++)
                {
                    var cellData = state.Board[r][c];
                    var viewModel = new CellViewModel(r, c)
                    {
                        PieceColor = cellData.PieceColor,
                        IsKing = cellData.IsKing,
                        IsSelected = false,     // Сброс выделения
                        IsPossibleMove = false  // Сброс подсветки
                    };
                    row.Add(viewModel);
                }
                Board.Add(row);
            }

            OnPropertyChanged(nameof(Board));
            OnPropertyChanged(nameof(CurrentPlayer));

            Status = state.IsGameOver
                ? $"Победа {state.Winner}! 🎉"
                : $"Ход: {_currentPlayer}. Выберите шашку.";
        }

        private void OnStateUpdated(GameState state)
        {
            _gameState = state;
            UpdateBoard(state);
            ClearSelection(); 
        }

        private void OnGameOver(string winner) => Status = $"Игра окончена! Победил {winner}";
        private void OnMoveRejected(string message) => Status = $"Недопустимый ход: {message}";

        public class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}
