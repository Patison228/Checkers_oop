using CheckersClient.ViewModels;
using System.Windows;

namespace CheckersClient.Views
{
    public partial class GameWindow : Window
    {
        public GameWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти в главное меню?\nИгра будет завершена.",
                "Шашки Онлайн",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                Close();
            }
        }
    }
}
