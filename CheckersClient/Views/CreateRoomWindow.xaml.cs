using CheckersClient.ViewModels;
using System.Windows;

namespace CheckersClient.Views
{
    public partial class CreateRoomWindow : Window
    {
        public CreateRoomWindow()
        {
            InitializeComponent();
            DataContext = new CreateRoomViewModel();
        }
    }
}
