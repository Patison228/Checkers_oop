using CheckersClient.ViewModels;
using System.Windows;

namespace CheckersClient.Views
{
    public partial class MainMenuWindow : Window
    {
        public MainMenuWindow()
        {
            InitializeComponent();
            DataContext = new MainMenuViewModel();
        }
    }
}
