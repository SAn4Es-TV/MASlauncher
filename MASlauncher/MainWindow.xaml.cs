using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
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
        DirectoryInfo MASpath;
        FileInfo exePath;
        public MainWindow()
        {
            InitializeComponent();


            if (!String.IsNullOrWhiteSpace(PathToMAS))
            {
                MASpath = new DirectoryInfo(PathToMAS);
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
                    FolderBrowserDialog masPath = new FolderBrowserDialog();
                    masPath.Description = "Выберите папку с MAS";
                    if(masPath.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        MASpath = new DirectoryInfo(masPath.SelectedPath);
                        Debug.Write(MASpath.Exists);
                        if (MASpath.Exists)
                        {
                            exePath = new FileInfo(MASpath.FullName + @"\DDLC.exe");
                            if (exePath.Exists)
                            {
                                Debug.Write(MASpath);
                                PathToMAS = masPath.SelectedPath;
                                DownloadTranslate();
                                type = 2;

                            }
                            else
                            {
                                System.Windows.MessageBox.Show("Отсутствует исполняемый файл", "Monika After Story" );
                            }
                        }
                    }
                    break;
                case 1:
                    UpdateTranslate();
                    break;
                case 2:
                    RunGame();
                    break;
                default:
                    break;
            }
            DownloadButt.Content = buttonText[type];
        }
        // ------ ФУНКЦИИ КНОПКИ ------
        // Скачать перевод
        void DownloadTranslate()
        {

        }
        // Запустить мод
        void RunGame()
        {

        }
        // Обновить перевод
        void UpdateTranslate()
        {

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
