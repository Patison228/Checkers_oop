using CheckersClient.ViewModels;
using System.Windows;

namespace CheckersClient.Views
{
    public partial class JoinRoomWindow : Window
    {
        public JoinRoomWindow()
        {
            InitializeComponent();
        }

        private JoinRoomViewModel Vm => (JoinRoomViewModel)DataContext;

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            Vm.Join();
        }
    }
}
