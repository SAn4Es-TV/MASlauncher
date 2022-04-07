using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MASlauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string PathToMAS = "";
        public string downloadString = "";
        public string[] buttonText = { "Установить", "Обновить", "Начать игру" };
        public int type = 2;
        public MainWindow()
        {
            InitializeComponent();


            if (!String.IsNullOrWhiteSpace(PathToMAS))
            {
                FileInfo MASpath = new FileInfo(PathToMAS);
                if (MASpath.Exists) type = 2;
                else type = 0;
            }
            else
            {
                type = 0;
            }

            DownloadProgress.Value = 0;
            DownloadProgress.Visibility = Visibility.Hidden;
            DownloadString.Content = downloadString;
            DownloadButt.Content = buttonText[type];
        }
        private void DownloadButt_Click(object sender, RoutedEventArgs e)
        {
            switch (type)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                default:
                    break;
            }
        }


        // ------ ЛОГИКА ОКНА ------
        // Движение мышкой
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // Скрываем окно
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        // Закрываем окно
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
