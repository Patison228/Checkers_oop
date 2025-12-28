using CheckersClient.Models;
using CheckersClient.Services;
using CheckersModels.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CheckersClient.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private SignalRService _signalRService;
        private GameState _gameState;
        private ObservableCollection<ObservableCollection<ObservableCell>> _board;
        private string _currentPlayer;
        private int? _selectedRow;
        private int? _selectedCol;

        public ObservableCollection<ObservableCollection<ObservableCell>> Board
        {
            get => _board;
            set => SetProperty(ref _board, value);
        }

        public string CurrentPlayer
        {
            get => _currentPlayer;
            set => SetProperty(ref _currentPlayer, value);
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
                    UpdateBoardDisplay();
                }
            }
        }

        public GameViewModel()
        {
            CellClickCommand = new RelayCommand(CellClick);
            Board = new ObservableCollection<ObservableCollection<ObservableCell>>();
        }

        private async void CellClick(object parameter)
        {
            if (parameter is string cellData && int.TryParse(cellData, out int index))
            {
                int row = index / 8;
                int col = index % 8;

                if (_selectedRow.HasValue && _selectedCol.HasValue)
                {
                    // Make move
                    var move = new Move
                    {
                        FromRow = _selectedRow.Value,
                        FromCol = _selectedCol.Value,
                        ToRow = row,
                        ToCol = col
                    };
                    await SignalRService.MakeMoveAsync(GameState.RoomId, move);
                    _selectedRow = null;
                    _selectedCol = null;
                }
                else if (GameState.Board[row, col].Color.ToString() == GameState.CurrentPlayer)
                {
                    // Select piece
                    _selectedRow = row;
                    _selectedCol = col;
                }
            }
        }

        private void UpdateBoardDisplay()
        {
            Board.Clear();
            if (GameState?.Board == null) return;

            for (int row = 0; row < 8; row++)
            {
                var rowCollection = new ObservableCollection<ObservableCell>();
                for (int col = 0; col < 8; col++)
                {
                    var cell = GameState.Board[row, col];
                    rowCollection.Add(new ObservableCell
                    {
                        Row = row,
                        Col = col,
                        Color = cell.Color,
                        Type = cell.Type,
                        IsSelected = row == _selectedRow && col == _selectedCol
                    });
                }
                Board.Add(rowCollection);
            }
            CurrentPlayer = $"Ход: {GameState.CurrentPlayer}";
        }
    }
}
