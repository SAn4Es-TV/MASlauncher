using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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

using static System.Net.Mime.MediaTypeNames;

namespace MASlauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string settingsPath = System.Windows.Forms.Application.StartupPath + @"\Settings.txt";
        public string PathToSetup = System.Windows.Forms.Application.StartupPath + @"\Setup.exe";
        public string PathToMASFolder = "";
        public string PathToMASExe = "";
        public string downloadString = "";
        public string settingsString = "";
        public string[] buttonText = { "Установить", "Обновить", "Начать игру" };
        public int type = 2;
        public bool settingsFlag = false;
        DirectoryInfo MASpath;
        FileInfo exePath;
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            if (!String.IsNullOrWhiteSpace(PathToMASFolder))
            {
                MASpath = new DirectoryInfo(PathToMASFolder);
                if (MASpath.Exists) type = 2;
                else type = 0;
            }
            else
            {
                type = 0;
            }

            type = 1;
            DownloadProgress.Value = 0;
            DownloadProgress.Visibility = Visibility.Hidden;
            DownloadString.Content = downloadString;
            DownloadButt.Content = buttonText[type];

            SolicenTEAM.Updater.ExeFileName = "MASlauncher";
            Task.Factory.StartNew(async () =>
            {
                    await SolicenTEAM.Updater.CheckUpdate("SAn4Es-TV", "MASlauncher");
                this.Dispatcher.Invoke(() =>
                {
                    ver.Text = "Версия: " + SolicenTEAM.Updater.CurrentVersion.ToString();
                });
            });
        }
        private void DownloadButt_Click(object sender, RoutedEventArgs e)
        {
            switch (type)
            {
                case 0:
                    InstallTranslate();
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
        // Установить перевод
        void InstallTranslate()
        {
            FolderBrowserDialog masPath = new FolderBrowserDialog();
            masPath.Description = "Выберите папку с MAS";
            if (masPath.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MASpath = new DirectoryInfo(masPath.SelectedPath);
                Debug.Write(MASpath.Exists);
                if (MASpath.Exists)
                {
                    exePath = new FileInfo(MASpath.FullName + @"\DDLC.exe");
                    if (exePath.Exists)
                    {
                        Debug.Write(MASpath);
                        PathToMASFolder = masPath.SelectedPath;
                        PathToMASExe = PathToMASFolder + @"\DDLC.exe";
                        SaveSettings();
                        DownloadTranslate();
                        type = 2;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Отсутствует исполняемый файл", "Monika After Story");
                    }
                }
            }
        }
        // Скачать перевод
        void DownloadTranslate()
        {
            UpdateTranslate();
        }
        // Запустить мод
        void RunGame()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = PathToMASExe;
            proc.UseShellExecute = true;
            proc.Verb = "runas";

            Process.Start(proc); // запускаем программу
            this.Close();
        }
        // Обновить перевод
        void UpdateTranslate()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = PathToSetup;
            proc.UseShellExecute = true;
            proc.Arguments = " -silent -PATH:\"" + PathToMASExe + "\"";
            proc.Verb = "runas";

            Process.Start(proc); // запускаем программу
            type = 2;
            DownloadButt.Content = buttonText[type];
        }

        // Сохранить настройки
        void SaveSettings()
        {
            settingsString = MASpath.FullName + '\n';
            // полная перезапись файла
            using (StreamWriter writer = new StreamWriter(settingsPath, false))
            {
               writer.WriteLine(settingsString);
            }
        }

        // Загрузить настройки
        void LoadSettings()
        {
            if (new FileInfo(settingsPath).Exists)
            {
                using (StreamReader reader = new StreamReader(settingsPath))
                {
                    string text = reader.ReadToEnd();

                    string[] data = text.Split('\n');
                    PathToMASFolder = data[0];
                    PathToMASExe = PathToMASFolder + @"\DDLC.exe";

                    PathToDir.Text = PathToMASFolder;
                }
            }
        }

        // ------- НАСТРОЙКИ -------
        // Изменить путь игры
        private void ChanceGameDir_Click(object sender, RoutedEventArgs e)
        {
            InstallTranslate();
        }
        // Проверить обновления клиента
        private void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateLauncherAsync();

        }

        public async Task UpdateLauncherAsync()
        {
            await SolicenTEAM.Updater.CheckUpdate("SAn4Es-TV", "MASlauncher");
            if (SolicenTEAM.Updater.UpdateVersion == SolicenTEAM.Updater.CurrentVersion && SolicenTEAM.Updater.UpdateVersion != "")
            {
                System.Windows.Forms.MessageBox.Show("Установлена новейшая версия програмного обеспечения!");

            }
            else
            {
                Debug.WriteLine(SolicenTEAM.Updater.UpdateDescription);
                Debug.WriteLine(SolicenTEAM.Updater.CurrentVersion);
                Debug.WriteLine(SolicenTEAM.Updater.UpdateVersion);
                if (SolicenTEAM.Updater.UpdateVersion != "")
                { await SolicenTEAM.Updater.CheckUpdate("SAn4Es-TV", "MASlauncher"); }

                SolicenTEAM.Updater.DownloadUpdate(SolicenTEAM.Updater.gitUser, SolicenTEAM.Updater.gitRepo);
                SolicenTEAM.Updater.ExtractArchive();
            }
        }
        // ------ ЛОГИКА ОКНА ------
        // Движение мышкой
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Открываем настройки
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            settingsFlag = !settingsFlag;
            if (settingsFlag)
                Settings.Visibility = Visibility.Visible;
            else
                Settings.Visibility = Visibility.Hidden;
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

        private void SideMenu_SelectionChanged(object sender, HandyControl.Data.FunctionEventArgs<object> e)
        {
        }

        private void SideMenuItem_Selected(object sender, RoutedEventArgs e)
        {
            HandyControl.Controls.SideMenuItem item = sender as HandyControl.Controls.SideMenuItem;
            string selectedItem = item.Name.ToString();

            switch (selectedItem)
            {
                case "Общие":
                    MainSettings.Visibility = Visibility.Visible;
                    break;
                case "О нас":
                    MainSettings.Visibility = Visibility.Hidden;
                    break;

            }

        }

    }
}
