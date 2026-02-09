using CheckersClient.Models;
using CheckersClient.Services;
using CheckersModels.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System;

namespace CheckersClient.ViewModels
{
    /// <summary>
    /// ViewModel логики игры с множественным взятием и обязательностью рубания.
    /// </summary>
    public class GameViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private GameState _gameState;
        private ObservableCollection<ObservableCollection<CellViewModel>> _board;
        private CellViewModel? _selectedCell;
        private string _status = "Ожидание подключения игроков...";
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
            _signalRService.StateUpdated += OnStateUpdated;
            _signalRService.GameOver += OnGameOver;
            _signalRService.MoveRejected += OnMoveRejected;

            UpdateBoard(initialState);
            CellClicked = new RelayCommand(OnCellClicked);
        }

        private void OnCellClicked(object? parameter)
        {
            if (parameter is not CellViewModel cell) return;

            if (_gameState.IsGameOver)
            {
                Status = $"Игра окончена! Победил {_gameState.Winner}";
                return;
            }

            // Если продолжается серия взятий - можно выбрать только указанную шашку
            if (_gameState.MustContinueCapture)
            {
                if (cell.Row == _gameState.ContinueCaptureFromRow &&
                    cell.Col == _gameState.ContinueCaptureFromCol)
                {
                    if (_selectedCell == cell)
                    {
                        ClearSelection();
                        Status = "Выбор отменён. Продолжите взятие!";
                    }
                    else
                    {
                        SelectPiece(cell);
                    }
                }
                else if (cell.IsPossibleMove || cell.IsMandatoryCapture)
                {
                    // Ход на подсвеченную клетку
                    if (_selectedCell != null)
                    {
                        MakeMove(_selectedCell, cell);
                    }
                }
                else
                {
                    Status = "Необходимо продолжить взятие текущей шашкой!";
                }
                return;
            }

            if (_selectedCell == null)
            {
                // Первый клик - выбор шашки
                if (cell.PieceColor == _gameState.CurrentPlayer)
                {
                    SelectPiece(cell);
                }
                else
                {
                    Status = "Выберите свою шашку";
                }
            }
            else
            {
                // Второй клик
                if (cell == _selectedCell)
                {
                    ClearSelection();
                    Status = "Выбор отменён";
                }
                else if (cell.IsPossibleMove || cell.IsMandatoryCapture)
                {
                    MakeMove(_selectedCell, cell);
                }
                else if (cell.PieceColor == _gameState.CurrentPlayer)
                {
                    // Выбрана другая своя шашка
                    SelectPiece(cell);
                }
                else
                {
                    ClearSelection();
                    Status = "Недопустимый ход";
                }
            }
        }

        private void SelectPiece(CellViewModel cell)
        {
            ClearSelection();
            _selectedCell = cell;
            cell.IsSelected = true;
            HighlightPossibleMoves(cell);

            var captures = GetPossibleCaptures(cell);
            if (captures.Count > 0)
            {
                Status = $"Шашка выбрана. Необходимо взять {captures.Count} шашек";
            }
            else
            {
                Status = "Шашка выбрана. Выберите ход";
            }
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
            foreach (var row in Board ?? new())
                foreach (var cell in row)
                {
                    cell.IsPossibleMove = false;
                    cell.IsMandatoryCapture = false;
                }
        }

        private void HighlightPossibleMoves(CellViewModel selected)
        {
            ClearHighlights();

            string playerColor = _gameState.CurrentPlayer;

            // Сначала проверяем взятия
            var allCaptures = GetAllPossibleCaptures(playerColor);

            if (_gameState.MustContinueCapture)
            {
                // Продолжение серии взятий - показываем только взятия для текущей шашки
                var captures = GetPossibleCaptures(selected);
                foreach (var capture in captures)
                {
                    var targetCell = GetCell(capture.ToRow, capture.ToCol);
                    targetCell.IsMandatoryCapture = true;
                }
            }
            else if (allCaptures.Count > 0)
            {
                // Есть взятия - показываем только взятия для выбранной шашки
                var captures = allCaptures.Where(c =>
                    c.FromRow == selected.Row && c.FromCol == selected.Col).ToList();

                foreach (var capture in captures)
                {
                    var targetCell = GetCell(capture.ToRow, capture.ToCol);
                    targetCell.IsMandatoryCapture = true;
                }
            }
            else
            {
                // Обычные ходы
                HighlightRegularMoves(selected, playerColor);
            }
        }

        private void HighlightRegularMoves(CellViewModel selected, string playerColor)
        {
            int[] rowDeltas = selected.IsKing ? new[] { -1, 1 } : new[] { playerColor == "White" ? -1 : 1 };
            int[] colDeltas = { -1, 1 };

            foreach (int rowDir in rowDeltas)
            {
                foreach (int colDir in colDeltas)
                {
                    int toRow = selected.Row + rowDir;
                    int toCol = selected.Col + colDir;

                    if (toRow >= 0 && toRow < 8 && toCol >= 0 && toCol < 8)
                    {
                        var toCell = GetCell(toRow, toCol);
                        if (toCell.PieceColor == "None")
                            toCell.IsPossibleMove = true;
                    }
                }
            }
        }

        private System.Collections.Generic.List<MoveRequest> GetPossibleCaptures(CellViewModel from)
        {
            var captures = new System.Collections.Generic.List<MoveRequest>();
            string playerColor = _gameState.CurrentPlayer;
            string opponent = playerColor == "White" ? "Black" : "White";

            int[] rowDirs = from.IsKing ? new[] { -1, 1 } : new[] { playerColor == "White" ? -1 : 1 };
            int[] colDirs = { -1, 1 };
            int[] colDeltas = { -1, 1 };

            foreach (int rowDir in rowDirs)
            {
                foreach (int colDir in colDeltas)
                {
                    int jumpRow = from.Row + rowDir * 2;
                    int jumpCol = from.Col + colDir * 2;

                    if (jumpRow >= 0 && jumpRow < 8 && jumpCol >= 0 && jumpCol < 8)
                    {
                        int midRow = from.Row + rowDir;
                        int midCol = from.Col + colDir;

                        var midCell = GetCell(midRow, midCol);
                        var targetCell = GetCell(jumpRow, jumpCol);

                        if (midCell.PieceColor == opponent && targetCell.PieceColor == "None")
                        {
                            captures.Add(new MoveRequest
                            {
                                FromRow = from.Row,
                                FromCol = from.Col,
                                ToRow = jumpRow,
                                ToCol = jumpCol
                            });
                        }
                    }
                }
            }

            return captures;
        }

        private System.Collections.Generic.List<MoveRequest> GetAllPossibleCaptures(string playerColor)
        {
            var allCaptures = new System.Collections.Generic.List<MoveRequest>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var cell = GetCell(row, col);
                    if (cell.PieceColor == playerColor)
                    {
                        var captures = GetPossibleCaptures(cell);
                        allCaptures.AddRange(captures);
                    }
                }
            }

            return allCaptures;
        }

        private CellViewModel GetCell(int row, int col) => Board[row][col];

        private void MakeMove(CellViewModel from, CellViewModel to)
        {
            bool isCapture = Math.Abs(to.Row - from.Row) == 2;

            if (isCapture)
            {
                Status = "Рубим шашку...";
            }
            else
            {
                Status = "Отправляю ход...";
            }

            ClearSelection();

            var move = new MoveRequest
            {
                RoomId = _gameState.RoomId,
                FromRow = from.Row,
                FromCol = from.Col,
                ToRow = to.Row,
                ToCol = to.Col
            };

            _signalRService.SendMakeMove(move);
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
                    row.Add(new CellViewModel(r, c)
                    {
                        PieceColor = cellData.PieceColor,
                        IsKing = cellData.IsKing
                    });
                }
                Board.Add(row);
            }

            OnPropertyChanged(nameof(Board));
            OnPropertyChanged(nameof(CurrentPlayer));

            if (state.IsGameOver)
            {
                Status = $"🏆 Победа {state.Winner}! 🏆";
            }
            else if (state.MustContinueCapture)
            {
                Status = "⚡ Продолжайте взятие! Выберите следующий ход ⚡";
            }
            else
            {
                Status = $"Ход: {state.CurrentPlayer}. Выберите свою шашку";
            }
        }

        private void OnStateUpdated(GameState state) => UpdateBoard(state);

        private void OnGameOver(string winner) => Status = $"🏆 Игра окончена! Победил {winner} 🏆";

        private void OnMoveRejected(string message) => Status = $"❌ Недопустимый ход: {message}";
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute) => _execute = execute;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;
    }
}
