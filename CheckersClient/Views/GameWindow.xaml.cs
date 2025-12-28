using CheckersClient.ViewModels;
using System.Windows;

namespace CheckersClient.Views
{
    public partial class GameWindow : Window
    {
        public GameViewModel GameViewModel => (GameViewModel)DataContext;

        public GameWindow()
        {
            InitializeComponent();
            DataContext = GameViewModel = new GameViewModel();
        }
    }
}
