using CheckersClient.Models;
using CheckersClient.Services;
using CheckersModels.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace CheckersClient.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private readonly SignalRService _signalRService;
        private GameState _gameState;
        private ObservableCollection<ObservableCollection<CellViewModel>> _board;
        private string _status = "Ваша очередь?";
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
                    var cell = state.Board[r][c];
                    var viewModel = new CellViewModel(r, c)
                    {
                        PieceColor = cell.PieceColor,
                        IsKing = cell.IsKing
                    };
                    row.Add(viewModel);
                }
                Board.Add(row);
            }

            OnPropertyChanged(nameof(CurrentPlayer));
            Status = state.IsGameOver ? $"Победа {state.Winner}!" : $"Ход: {_currentPlayer}";
        }

        private void OnCellClicked(object? parameter)
        {
            if (parameter is not CellViewModel cellVm)
                return;

            int row = cellVm.Row;
            int col = cellVm.Col;

            // простая логика: ход своей шашкой
            if (cellVm.PieceColor == _gameState.CurrentPlayer)
            {
                var move = new MoveRequest
                {
                    RoomId = _gameState.RoomId,
                    FromRow = row,
                    FromCol = col,
                    ToRow = _gameState.CurrentPlayer == "White" ? row - 1 : row + 1,
                    ToCol = col + 1
                };

                if (move.ToRow >= 0 && move.ToRow < 8 &&
                    move.ToCol >= 0 && move.ToCol < 8)
                {
                    _signalRService.SendMakeMove(move);
                    Status = "Ход отправлен...";
                }
            }
        }

        private void OnStateUpdated(GameState state) => UpdateBoard(state);
        private void OnGameOver(string winner) => Status = $"Игра окончена! Победил {winner}";
        private void OnMoveRejected(string message) => Status = $"Недопустимый ход: {message}";
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}
