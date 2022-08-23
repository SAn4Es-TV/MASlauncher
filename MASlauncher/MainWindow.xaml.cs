using HandyControl.Controls;

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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using static System.Net.Mime.MediaTypeNames;

namespace MASlauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public string settingsPath = System.Windows.Forms.Application.StartupPath + @"\Settings.txt";
        public string PathToSetup = System.Windows.Forms.Application.StartupPath + @"\Setup.exe";
        public string PathToTranslate = System.Windows.Forms.Application.StartupPath + @"\";
        public string PathToMASFolder = "";
        public string PathToMASExe = "";
        public string downloadString = "";
        public string settingsString = "";
        public string[] buttonText = { "Установить", "Обновить", "Начать игру" };
        public int type = 2;
        public bool settingsFlag = false;
        DirectoryInfo MASpath;
        FileInfo exePath; 
        public static bool IsNight => DateTime.Now.Hour > 22 ||
                                        DateTime.Now.Hour < 6;               // Проверка День/Ночь


        string CurrentVersion;

        public DoubleAnimation _start;  // Анимация запуска
        public DoubleAnimation _quit;   // Анимация выхода

        SolicenTEAM.Updater launcherUpdater = new SolicenTEAM.Updater();
        SolicenTEAM.Updater translateUpdater = new SolicenTEAM.Updater();

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            if (IsNight)
                ___Monika_png.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/monikaroomdaylight.jpg"));
            else
                ___Monika_png.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/monikaroomnight.png"));
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

            translateUpdater.IniName = "translate.ini";

            CheckTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);

            DownloadButt.Content = buttonText[type];

            DownloadProgress.Value = 0;
            DownloadProgress.Visibility = Visibility.Hidden;
            //DownloadString.Content = downloadString;
            DownloadButt.Content = buttonText[type];

            launcherUpdater.ExeFileName = "MASlauncher";
            Task.Factory.StartNew(async () =>
            {
                    await launcherUpdater.CheckUpdate("SAn4Es-TV", "MASlauncher");
                this.Dispatcher.Invoke(() =>
                {
                    ver.Text = "Версия клиента: " + launcherUpdater.CurrentVersion.ToString();
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
        async void DownloadTranslate()
        {
            DownloadTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);
            while (!translateUpdater.readyToInstall) await Task.Delay(100);
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
        async Task CheckTranslateUpdate(string user, string repo, string path = "")
        {
            if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\" + "translate.ini"))
                CurrentVersion = File.ReadAllText(System.Windows.Forms.Application.StartupPath + "\\" + "translate.ini");

            await Task.Delay(10);
            translateUpdater.gitUser = user;
            translateUpdater.gitRepo = repo;

            await translateUpdater.GetUpdateVersion();
            await Task.Delay(10);
            CurrentVersion = File.ReadAllText(System.Windows.Forms.Application.StartupPath + "\\" + "translate.ini");

            Debug.WriteLine("This translate version: " + CurrentVersion);
            Debug.WriteLine("New translate version: " + translateUpdater.UpdateVersion);
            if (translateUpdater.UpdateVersion != CurrentVersion && translateUpdater.UpdateVersion != "")
            {
                Debug.WriteLine("New translate detected!");
                string s = translateUpdater.UpdateDescription;
                type = 1;
            }
            DownloadButt.Content = buttonText[type];
        }
        async Task DownloadTranslateUpdate(string user, string repo, string path = "")
        {
            if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\" + "translate.ini"))
                CurrentVersion = File.ReadAllText(System.Windows.Forms.Application.StartupPath + "\\" + "translate.ini");

            await Task.Delay(10);
            translateUpdater.gitUser = user;
            translateUpdater.gitRepo = repo;

            await translateUpdater.GetUpdateVersion();
            await Task.Delay(10);
            CurrentVersion = File.ReadAllText(System.Windows.Forms.Application.StartupPath + "\\" + "translate.ini");

            Debug.WriteLine("This translate version: " + CurrentVersion);
            Debug.WriteLine("New translate version: " + translateUpdater.UpdateVersion);
            if (translateUpdater.UpdateVersion != CurrentVersion && translateUpdater.UpdateVersion != "")
            {
                while (!translateUpdater.UpdateDescriptionReady) await Task.Delay(100);
                string s = translateUpdater.UpdateDescription;
                translateUpdater.solicenBar = DownloadProgress;
                translateUpdater.DownloadUpdate("SAn4Es-TV", "MAS-Russifier-NEW");
                translateUpdater.ExctractArchive(path);
            }
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
            await launcherUpdater.CheckUpdate("SAn4Es-TV", "MASlauncher");
            if (launcherUpdater.UpdateVersion == launcherUpdater.CurrentVersion && launcherUpdater.UpdateVersion != "")
            {
                System.Windows.Forms.MessageBox.Show("Установлена новейшая версия програмного обеспечения!");

            }
            else
            {
                //Debug.WriteLine(launcherUpdater.UpdateDescription);
                //Debug.WriteLine(launcherUpdater.CurrentVersion);
                //Debug.WriteLine(launcherUpdater.UpdateVersion);
                if (launcherUpdater.UpdateVersion != "")
                { await launcherUpdater.CheckUpdate("SAn4Es-TV", "MASlauncher"); }

                launcherUpdater.DownloadUpdate(launcherUpdater.gitUser, launcherUpdater.gitRepo);
                launcherUpdater.ExctractArchive(System.Windows.Forms.Application.StartupPath + "\\", true);
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
            {
                Settings.Visibility = Visibility.Visible;
                DoubleAnimation start = new DoubleAnimation();
                start.From = 0;
                start.To = 1;
                start.RepeatBehavior = new RepeatBehavior(1);
                start.Duration = new Duration(TimeSpan.FromMilliseconds(200));
                Settings.BeginAnimation(OpacityProperty, start);
            }
            else
            {
                DoubleAnimation end = new DoubleAnimation();
                end.From = 1;
                end.To = 0;
                end.RepeatBehavior = new RepeatBehavior(1);
                end.Duration = new Duration(TimeSpan.FromMilliseconds(200));
                end.Completed += (s, a) =>
                {
                    Settings.Visibility = Visibility.Hidden;
                };
                Settings.BeginAnimation(OpacityProperty, end);
            }
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

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            GoToURL("https://discord.com/invite/ZJ3SQpV");
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            GoToURL("https://discord.gg/C3EyszK59m");
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            GoToURL("https://github.com/DenisSolicen/Solicen-Projects");

        }

        private void Android_Click(object sender, RoutedEventArgs e)
        {
            GoToURL("https://github.com/DenisSolicen/Solicen-Projects/releases");

        }

        private void License_Click(object sender, RoutedEventArgs e)
        {
            BoxTitle.Text = "Правовой аспект";
            BoxText.Text = "Используемое название «Monika After Story» (MAS) является индикатором, для чего предназначен лаунчер, и мы никак не претендуем на сам «Monika After Story», команду разработчиков и их труды. Нам принадлежат только переводы созданные нами, и ресурсы созданные нами.\n" +
            "Мы не разработчики MAS, и нам не принадлежит ни единая часть оригинальных ресурсов «Monika After Story».\n" +
            "Данный лаунчер использует ресурсы, а именно фоны комнаты Моники в дневное, и ночное время -созданные командой MAS, для их мода Monika After Story, мы используем их на добровольной основе указывая источник их происхождения в нашем лаунчере.\n";
            Box.Visibility = Visibility.Visible;
            DoubleAnimation start = new DoubleAnimation();
            start.From = 0;
            start.To = 1;
            start.RepeatBehavior = new RepeatBehavior(1);
            start.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            Box.BeginAnimation(OpacityProperty, start);

        }

        private void Creators_Click(object sender, RoutedEventArgs e)
        {
            BoxTitle.Text = "Создатели";
            BoxText.Text = "Создатели лаунчера -  SAn4Es_TV и Denis Solicen\n" +
            "Права на перевод - Команда Солицена(SolicenTEAM)\n" +
            "Фоны комнаты -команда разработчиков «Monika After Story»";
            Box.Visibility= Visibility.Visible;
            DoubleAnimation start = new DoubleAnimation();
            start.From = 0;
            start.To = 1;
            start.RepeatBehavior = new RepeatBehavior(1);
            start.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            Box.BeginAnimation(OpacityProperty, start);
        }

        public void GoToURL(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DoubleAnimation end = new DoubleAnimation();
            end.From = 1;
            end.To = 0;
            end.RepeatBehavior = new RepeatBehavior(1);
            end.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            end.Completed += (s, a) =>
            {
                Box.Visibility = Visibility.Hidden;
            };
            Box.BeginAnimation(OpacityProperty, end);
        }
    }
}
