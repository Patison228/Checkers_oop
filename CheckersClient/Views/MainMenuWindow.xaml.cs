using CheckersClient.ViewModels;
using System.Windows;

namespace CheckersClient.Views
{
    public partial class MainMenuWindow : Window
    {
        public MainMenuWindow()
        {
            InitializeComponent();
        }

        private MainMenuViewModel Vm => (MainMenuViewModel)DataContext;

        private void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            Vm.OpenCreateRoom();
            this.Close();
        }

        private void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            Vm.OpenJoinRoom();
            this.Close();
        }
    }
}
