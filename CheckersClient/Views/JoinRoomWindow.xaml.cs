using CheckersClient.ViewModels;
using System.Windows;

namespace CheckersClient.Views
{
    public partial class JoinRoomWindow : Window
    {
        public JoinRoomWindow()
        {
            InitializeComponent();
            DataContext = new JoinRoomViewModel();
        }
    }
}
