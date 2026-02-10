using CheckersClient.Models;
using CheckersClient.Services;
using CheckersModels.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System;
using System.Collections.Generic;

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
        private readonly string _myPlayerColor;

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
        public string MyPlayerColor => _myPlayerColor;

        public ICommand CellClicked { get; }

        public GameViewModel(SignalRService signalRService, GameState initialState, string myPlayerColor)
        {
            _signalRService = signalRService;
            _myPlayerColor = myPlayerColor;
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

            if (_gameState.CurrentPlayer != _myPlayerColor)
            {
                Status = $"Сейчас ход противника ({_gameState.CurrentPlayer}). Ждите своего хода.";
                return;
            }

            if (_selectedCell == null)
            {
                
                if (_gameState.MustContinueCapture)
                {
                    
                    if (cell.Row == _gameState.ContinueCaptureFromRow &&
                        cell.Col == _gameState.ContinueCaptureFromCol)
                    {
                        SelectPiece(cell);
                    }
                    else
                    {
                        Status = "Необходимо продолжить взятие указанной шашкой!";
                    }
                    return;
                }

                
                if (cell.PieceColor == "None")
                {
                    Status = "Выберите свою шашку для хода";
                    return;
                }

                if (cell.PieceColor != _myPlayerColor)
                {
                    Status = $"Это шашка противника ({cell.PieceColor}). Выберите свою шашку ({_myPlayerColor})";
                    return;
                }

                SelectPiece(cell);
                return;
            }

            

            if (cell == _selectedCell)
            {
                ClearSelection();
                Status = "Выбор отменён";
                return;
            }

            if (_gameState.MustContinueCapture)
            {
                if (cell.IsMandatoryCapture)
                {
                    MakeMove(_selectedCell, cell);
                }
                else
                {
                    Status = "Необходимо продолжить взятие! Выберите подсвеченную клетку";
                }
                return;
            }

            if (cell.IsPossibleMove || cell.IsMandatoryCapture)
            {
                MakeMove(_selectedCell, cell);
                return;
            }

            if (cell.PieceColor == _myPlayerColor)
            {
                SelectPiece(cell);
                return;
            }

            ClearSelection();
            Status = "Недопустимый ход. Выберите свою шашку";
        }

        private void SelectPiece(CellViewModel cell)
        {
            if (cell.PieceColor != _myPlayerColor)
            {
                Status = "Ошибка: нельзя выбрать чужую шашку!";
                return;
            }

            ClearSelection();
            _selectedCell = cell;
            cell.IsSelected = true;
            HighlightPossibleMoves(cell);

            var allPossibleCaptures = GetAllPossibleCaptures(_myPlayerColor);

            if (allPossibleCaptures.Count > 0)
            {
                var myCaptures = allPossibleCaptures.Where(c =>
                    c.FromRow == cell.Row && c.FromCol == cell.Col).ToList();

                if (myCaptures.Count > 0)
                {
                    Status = $"Шашка выбрана. Обязательное взятие: {myCaptures.Count} вариант(ов)";
                }
                else
                {
                    ClearSelection();
                    Status = "У этой шашки нет обязательных взятий. Выберите другую шашку";
                }
            }
            else
            {
                bool hasMoves = CheckRegularMoves(cell);
                if (hasMoves)
                {
                    Status = "Шашка выбрана. Выберите клетку для хода";
                }
                else
                {
                    ClearSelection();
                    Status = "У этой шашки нет возможных ходов. Выберите другую шашку";
                }
            }
        }

        private bool CheckRegularMoves(CellViewModel cell)
        {
            
            int[] rowDeltas;
            if (cell.IsKing)
            {
                rowDeltas = new[] { -1, 1 }; 
            }
            else
            {
                
                rowDeltas = _myPlayerColor == "White"
                    ? new[] { -1 } 
                    : new[] { 1 }; 
            }

            int[] colDeltas = { -1, 1 };

            foreach (int rowDir in rowDeltas)
            {
                foreach (int colDir in colDeltas)
                {
                    int toRow = cell.Row + rowDir;
                    int toCol = cell.Col + colDir;

                    if (toRow >= 0 && toRow < 8 && toCol >= 0 && toCol < 8)
                    {
                        var toCell = GetCell(toRow, toCol);
                        if (toCell.PieceColor == "None")
                            return true;
                    }
                }
            }

            return false;
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
            if (Board == null) return;

            foreach (var row in Board)
                foreach (var cell in row)
                {
                    cell.IsPossibleMove = false;
                    cell.IsMandatoryCapture = false;
                }
        }

        private void HighlightPossibleMoves(CellViewModel selected)
        {
            ClearHighlights();

            var allPossibleCaptures = GetAllPossibleCaptures(_myPlayerColor);

            if (_gameState.MustContinueCapture)
            {
                var captures = GetPossibleCaptures(selected);
                foreach (var capture in captures)
                {
                    var targetCell = GetCell(capture.ToRow, capture.ToCol);
                    targetCell.IsMandatoryCapture = true;
                }
            }
            else if (allPossibleCaptures.Count > 0)
            {
                var captures = allPossibleCaptures.Where(c =>
                    c.FromRow == selected.Row && c.FromCol == selected.Col).ToList();

                foreach (var capture in captures)
                {
                    var targetCell = GetCell(capture.ToRow, capture.ToCol);
                    targetCell.IsMandatoryCapture = true;
                }
            }
            else
            {
                HighlightRegularMoves(selected, _myPlayerColor);
            }
        }

        private void HighlightRegularMoves(CellViewModel selected, string playerColor)
        {
            
            int[] rowDeltas;
            if (selected.IsKing)
            {
                rowDeltas = new[] { -1, 1 }; 
            }
            else
            {
                
                rowDeltas = playerColor == "White"
                    ? new[] { -1 }
                    : new[] { 1 }; 
            }

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

        /// <summary>
        /// Возвращает возможные взятия для конкретной шашки.
        /// </summary>
        private List<MoveRequest> GetPossibleCaptures(CellViewModel from)
        {
            var captures = new List<MoveRequest>();
            string opponent = _myPlayerColor == "White" ? "Black" : "White";

            List<(int rowDir, int colDir)> directions = new()
            {
                (-1, -1), (-1, 1), (1, -1), (1, 1)
            };

            foreach (var (rowDir, colDir) in directions)
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

            return captures;
        }

        private List<MoveRequest> GetAllPossibleCaptures(string playerColor)
        {
            var allCaptures = new List<MoveRequest>();

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

            var newBoard = new ObservableCollection<ObservableCollection<CellViewModel>>();
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
                newBoard.Add(row);
            }

            Board = newBoard;

            OnPropertyChanged(nameof(Board));
            OnPropertyChanged(nameof(CurrentPlayer));

            if (state.IsGameOver)
            {
                Status = $"Игра окончена! Победил {state.Winner}!";
                ClearSelection();
            }
            else if (state.MustContinueCapture)
            {
                if (state.CurrentPlayer == _myPlayerColor)
                {
                    Status = "Продолжайте взятие! Выберите следующий ход";
                    if (state.ContinueCaptureFromRow >= 0 && state.ContinueCaptureFromCol >= 0)
                    {
                        var continueCell = GetCell(state.ContinueCaptureFromRow, state.ContinueCaptureFromCol);
                        if (continueCell != null && continueCell.PieceColor == _myPlayerColor)
                        {
                            SelectPiece(continueCell);
                        }
                    }
                }
                else
                {
                    Status = $"Противник продолжает взятие. Ждите своего хода.";
                    ClearSelection();
                }
            }
            else
            {
                if (state.CurrentPlayer == _myPlayerColor)
                {
                    Status = $"Ваш ход ({_myPlayerColor}). Выберите свою шашку";
                }
                else
                {
                    Status = $"Ход противника ({state.CurrentPlayer}). Ждите своего хода.";
                    ClearSelection();
                }
            }
        }

        private void OnStateUpdated(GameState state) => UpdateBoard(state);

        private void OnGameOver(string winner)
        {
            Status = $"Игра окончена! Победил {winner}";
            ClearSelection();
        }

        private void OnMoveRejected(string message)
        {
            Status = $"Ошибка: {message}";
            ClearSelection();
        }
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