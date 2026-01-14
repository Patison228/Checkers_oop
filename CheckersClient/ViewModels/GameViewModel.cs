using CheckersClient.Models;
using CheckersClient.Services;
using CheckersModels.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace CheckersClient.ViewModels
{
    /// <summary>
    /// ViewModel логики игры в шашки.
    /// Управляет игровым состоянием, обработкой кликов,
    /// выделением шашек, возможных ходов и взаимодействием с SignalR-сервисом.
    /// </summary>
    public class GameViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private GameState _gameState;
        private ObservableCollection<ObservableCollection<CellViewModel>> _board;
        private CellViewModel? _selectedCell;
        private string _status = "Выберите свою шашку";
        private string _currentPlayer = "";

        /// <summary>
        /// Текущее состояние игровой доски (8x8 клеток).
        /// Каждая клетка представлена через <see cref="CellViewModel"/>.
        /// </summary>
        public ObservableCollection<ObservableCollection<CellViewModel>> Board
        {
            get => _board;
            set { _board = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Текстовое сообщение для отображения текущего статуса игры.
        /// </summary>
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Имя текущего игрока ("White" или "Black").
        /// </summary>
        public string CurrentPlayer => _currentPlayer;

        /// <summary>
        /// Команда, вызываемая при клике по клетке доски.
        /// </summary>
        public ICommand CellClicked { get; }

        /// <summary>
        /// Конструктор ViewModel игры.
        /// Подключает SignalR-события и инициализирует игровое состояние.
        /// </summary>
        /// <param name="signalRService">Сервис связи с сервером SignalR.</param>
        /// <param name="initialState">Начальное состояние игры, включая расположение шашек.</param>
        public GameViewModel(SignalRService signalRService, GameState initialState)
        {
            _signalRService = signalRService;
            _signalRService.StateUpdated += OnStateUpdated;
            _signalRService.GameOver += OnGameOver;
            _signalRService.MoveRejected += OnMoveRejected;

            UpdateBoard(initialState);
            CellClicked = new RelayCommand(OnCellClicked);
        }

        /// <summary>
        /// Обработка кликов по клеткам доски:
        /// выбор шашки, начало хода или отмена выделения.
        /// </summary>
        private void OnCellClicked(object? parameter)
        {
            if (parameter is not CellViewModel cell) return;

            if (_selectedCell == null)
            {
                // Первый клик — выбор своей шашки
                if (cell.PieceColor == _gameState.CurrentPlayer)
                {
                    SelectPiece(cell);
                }
            }
            else
            {
                // Второй клик — попытка сделать ход или отменить выбор
                if (cell == _selectedCell)
                {
                    ClearSelection();
                    Status = "Выбор отменён";
                }
                else if (IsValidMove(_selectedCell, cell))
                {
                    MakeMove(_selectedCell, cell);
                }
                else
                {
                    ClearSelection();
                    Status = "Недопустимый ход";
                }
            }
        }

        /// <summary>
        /// Выделяет выбранную шашку и подсвечивает допустимые ходы.
        /// </summary>
        private void SelectPiece(CellViewModel cell)
        {
            ClearSelection();
            _selectedCell = cell;
            cell.IsSelected = true;
            HighlightPossibleMoves(cell);
            Status = "Шашка выбрана. Выберите ход";
        }

        /// <summary>
        /// Снимает выделение с текущей выбранной шашки и сбрасывает подсветку.
        /// </summary>
        private void ClearSelection()
        {
            if (_selectedCell != null)
            {
                _selectedCell.IsSelected = false;
                ClearHighlights();
                _selectedCell = null;
            }
        }

        /// <summary>
        /// Убирает все подсветки возможных ходов на доске.
        /// </summary>
        private void ClearHighlights()
        {
            foreach (var row in Board ?? new())
                foreach (var cell in row)
                    cell.IsPossibleMove = false;
        }

        /// <summary>
        /// Подсвечивает клетки, куда выбранная шашка может пойти
        /// (включая обычные ходы и возможные взятия).
        /// </summary>
        private void HighlightPossibleMoves(CellViewModel selected)
        {
            ClearHighlights();

            string playerColor = _gameState.CurrentPlayer;
            int[] rowDeltas = selected.IsKing ? new[] { -1, 1 } : new[] { playerColor == "White" ? -1 : 1 };
            int[] colDeltas = { -1, 1 };

            // Простые ходы
            foreach (int rowDir in rowDeltas)
                foreach (int colDir in colDeltas)
                    CheckMove(selected, rowDir, colDir);

            // Возможные взятия
            foreach (int rowDir in rowDeltas)
                foreach (int colDir in colDeltas)
                    CheckCapture(selected, rowDir * 2, colDir * 2);
        }

        /// <summary>
        /// Проверяет возможность обычного шага (без взятия).
        /// </summary>
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

        /// <summary>
        /// Проверяет возможность хода с взятием шашки соперника.
        /// </summary>
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

        /// <summary>
        /// Возвращает ячейку по заданным координатам.
        /// </summary>
        private CellViewModel GetCell(int row, int col) => Board[row][col];

        /// <summary>
        /// Проверяет корректность выбранного хода
        /// (перемещение на одну клетку или взятие шашки противника).
        /// </summary>
        private bool IsValidMove(CellViewModel from, CellViewModel to)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Col - from.Col);

            // Обычный ход
            if (rowDiff == 1 && colDiff == 1)
            {
                if (from.IsKing) return true;
                int expectedDir = _gameState.CurrentPlayer == "White" ? -1 : 1;
                return (to.Row - from.Row) == expectedDir;
            }

            // Ход с взятием шашки
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

        /// <summary>
        /// Отправляет запрос хода на сервер через SignalR.
        /// </summary>
        private void MakeMove(CellViewModel from, CellViewModel to)
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

            _signalRService.SendMakeMove(move);
        }

        /// <summary>
        /// Обновляет состояние доски и игрока на основе данных с сервера.
        /// </summary>
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
            Status = state.IsGameOver ? $"Победа {state.Winner}!" : "Выберите свою шашку";
        }

        /// <summary>
        /// Обработчик обновления состояния игры от сервера.
        /// </summary>
        private void OnStateUpdated(GameState state) => UpdateBoard(state);

        /// <summary>
        /// Обработчик завершения игры.
        /// </summary>
        private void OnGameOver(string winner) => Status = $"Игра окончена! Победил {winner}";

        /// <summary>
        /// Обработчик при отклонении хода сервером.
        /// </summary>
        private void OnMoveRejected(string message) => Status = $"Недопустимый ход: {message}";
    }

    /// <summary>
    /// Универсальная реализация команды для биндинга действий в WPF.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        /// <summary>
        /// Создаёт новую команду на основе указанного действия.
        /// </summary>
        public RelayCommand(Action<object?> execute) => _execute = execute;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;
    }
}
