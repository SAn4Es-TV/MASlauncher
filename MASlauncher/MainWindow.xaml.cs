using HandyControl.Controls;

using Ionic.Zip;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
using Microsoft.Toolkit.Uwp.Notifications;

using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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
        public string PathToPersistent = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\RenPy\Monika After Story";
        public string PathToPersistentBackup = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\RenPy\MAS-Backup";
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
        string MASCurrentVersion;

        public DoubleAnimation _start;  // Анимация запуска
        public DoubleAnimation _quit;   // Анимация выхода

        SolicenTEAM.Updater launcherUpdater = new SolicenTEAM.Updater();
        SolicenTEAM.Updater translateUpdater = new SolicenTEAM.Updater();
        SolicenTEAM.Updater masUpdater = new SolicenTEAM.Updater();

        string ddlcLink = "https://drive.google.com/u/0/uc?id=1o6urVBdzI1K8KY_z_VrhaYqZZ-dUfZnq&export=download&confirm=t&uuid=108a5d75-09cd-422a-ac88-4ee057586531";

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            if (!IsNight)
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

            translateUpdater.IniName = "translate.ini";
            masUpdater.IniName = "mas.ini";
            masUpdater.endsWitch = "Mod.zip\"";

            //CheckTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);

            //DownloadButt.Content = buttonText[type];

            DownloadProgress.Value = 0;
            DownloadProgress.Visibility = Visibility.Hidden;
            //DownloadString.Content = downloadString;
           // DownloadButt.Content = buttonText[type];
            translateUpdater.solicenBar = DownloadProgress;
            translateUpdater.solicenBarText = DownloadData;

            launcherUpdater.ExeFileName = "MASlauncher";
            PersistentPath.Text = PathToPersistent;
            PathToDir.Text = PathToMASFolder;
            Task.Factory.StartNew(async () =>
            {
                    await launcherUpdater.CheckUpdate("SAn4Es-TV", "MASlauncher");
                this.Dispatcher.Invoke(() =>
                {
                    ver.Text = "Версия клиента: " + launcherUpdater.CurrentVersion.ToString();
                });
            });

            _ = Task.Run(async () =>
            {
                while (true)
                {

                    await CheckTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);
                    Task.Delay(300000).Wait();
                }
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
                    DownloadTranslate();
                    break;
                case 2:
                    RunGame();
                    break;
                default:
                    break;
            }
            DownloadButt.Content = buttonText[type];
        }

        #region ------ ФУНКЦИИ КНОПКИ ------
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
                        PathToDir.Text = PathToMASFolder;
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
            lockUnlockButtons(false);
            DownloadTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);
            while (!translateUpdater.readyToInstall) await Task.Delay(100);
            UpdateTranslate();
            lockUnlockButtons(true);
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
            //this.Close();
        }
        // Обновить перевод
        void UpdateTranslate()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = PathToSetup;
            proc.UseShellExecute = true;
            proc.Arguments = " -offshort -silent -PATH:\"" + PathToMASExe + "\"";
            proc.Verb = "runas";

            Process.Start(proc); // запускаем программу
            type = 2;
            DownloadButt.Content = buttonText[type];
        }
        async Task CheckTranslateUpdate(string user, string repo, string path = "")
        {
            Debug.WriteLine("-= Checking Translate Updates =-");
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
                new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddArgument("conversationId", 9813)
                    .AddText("Новая версия доступна!")
                    .AddText("Зайдите в лаунчер и нажмите кнопку \"Обновить\"")
                    .Show();
            }
            Dispatcher.Invoke(() =>
            {
                DownloadButt.Content = buttonText[type];
            });
        }
        async Task DownloadTranslateUpdate(string user, string repo, string path = "")
        {
            Debug.WriteLine("-= Downloading Translate =-");
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
                translateUpdater.DownloadUpdate("DenisSolicen", "MAS-Russifier-NEW");
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
        #endregion
        #region ------- НАСТРОЙКИ -------
        // Изменить путь игры
        private void ChanceGameDir_Click(object sender, RoutedEventArgs e)
        {
            InstallTranslate();
        }
        private void OpenGameFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", PathToMASFolder);
        }
        // Проверить обновления клиента
        private void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateLauncherAsync();

        }
        private void DeletePersistent_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo MASbackup = new DirectoryInfo(PathToPersistentBackup);
            DirectoryInfo MASpersistent = new DirectoryInfo(PathToPersistent);
            if(!MASbackup.Exists)
                MASbackup.Create();
            if(MASpersistent.Exists)
                MASpersistent.MoveTo(MASbackup + @"\" + DateTime.Now.ToString("MM.dd.yyyy H-mm"));
        }

        private void OpenPersistent_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", PathToPersistent.Replace("Monika After Story", ""));
        }
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private async void Reinstall_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\ddlc-win.zip"))
            {
                if (File.Exists(PathToMASFolder + "\\game\\script-topics.rpyc"))
                {
                    string pathToArchive = System.Windows.Forms.Application.StartupPath + "\\ddlc-win.zip";
                    if (Directory.Exists(PathToMASFolder + "\\game"))
                    {
                        Directory.Delete(PathToMASFolder + "\\game", true);
                    }
                    try
                    {
                        if (!File.Exists(pathToArchive))
                        {
                            Debug.WriteLine("DDLC archive in " + pathToArchive + " not found");
                            return;
                        }
                        lockUnlockButtons(false);
                        string extractPath = PathToMASFolder;
                        using (ZipFile zip = ZipFile.Read(pathToArchive))
                        {
                            try
                            {
                                zip.ToList().ForEach(ze =>
                                {
                                    if (ze.FileName.Contains("game")
                                        && !ze.FileName.Contains("renpy")
                                        && !ze.FileName.Contains("lib"))
                                    {
                                        ze.FileName = ze.FileName.Replace("DDLC-1.1.1-pc", "");
                                        Debug.WriteLine("Extracting " + ze.FileName);
                                        ze.Extract(extractPath);
                                    }
                                });
                            }
                            catch { }
                        }
                        //File.Delete(pathToArchive);


                        if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\" + "mas.ini"))
                            MASCurrentVersion = File.ReadAllText(System.Windows.Forms.Application.StartupPath + "\\" + "mas.ini");

                        await Task.Delay(10);
                        masUpdater.gitUser = "Monika-After-Story";
                        masUpdater.gitRepo = "MonikaModDev";
                        masUpdater.solicenBarText = DownloadData;
                        masUpdater.solicenBar = DownloadProgress;

                        await masUpdater.GetUpdateVersion();
                        await Task.Delay(10);

                        Debug.WriteLine("This mas version: " + MASCurrentVersion);
                        Debug.WriteLine("New mas version: " + masUpdater.UpdateVersion);

                        while (!masUpdater.UpdateDescriptionReady) await Task.Delay(100);
                        Debug.WriteLine(masUpdater.UpdateDescriptionReady);
                        Debug.WriteLine(translateUpdater.UpdateDescriptionReady);
                        string d = masUpdater.UpdateDescription;
                        masUpdater.DownloadUpdate("Monika-After-Story", "MonikaModDev");
                        masUpdater.ExctractArchive(PathToMASFolder + "\\game");
                        if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\translate.ini")) 
                            File.Delete(System.Windows.Forms.Application.StartupPath + "\\translate.ini");

                        while (!masUpdater.readyToInstall) await Task.Delay(100);

                        DownloadTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);

                        while (!translateUpdater.readyToInstall) await Task.Delay(100);
                        UpdateTranslate();
                        lockUnlockButtons(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ERROR: " + ex);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Для полноценной переустановки нам требуется наличие файлов MonikaAfterStory в папке с игрой, пожалуйста скачайте MonikaAfterStory с их сайта, и поместите в папку game внутри папки с игрой для работы переустановки игры через лаунчер.\n" +
                "Благодарим за понимание.", "Monika After Story");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Для полноценной переустановки нам требуется наличие архива ddlc-win.zip в папке с лаунчером, пожалуйста скачайте официальную копию DDLC с их сайта, и поместите в папку лаунчера для работы переустановки игры через лаунчер.\n" +
                "Благодарим за понимание.", "Monika After Story");
            }
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
        #endregion
        #region ------ ЛОГИКА ОКНА ------
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
        #endregion
        #region ------ БОКОВАЯ ПАНЕЛЬ ------
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
        private void sidePanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            DoubleAnimation buttonAnimation = new DoubleAnimation();
            buttonAnimation.From = sidePanel.ActualWidth;
            buttonAnimation.To = 300;
            buttonAnimation.Duration = TimeSpan.FromMilliseconds(200);
            sidePanel.BeginAnimation(Grid.WidthProperty, buttonAnimation);
            /*
            DoubleAnimation fade = new DoubleAnimation();
            fade.From = sidePanel.Opacity;
            fade.To = 1;
            fade.Duration = TimeSpan.FromMilliseconds(100);
            sidePanel.BeginAnimation(OpacityProperty, fade);*/
        }

        private void sidePanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            DoubleAnimation buttonAnimation = new DoubleAnimation();
            buttonAnimation.From = sidePanel.ActualWidth;
            buttonAnimation.To = 90;
            buttonAnimation.Duration = TimeSpan.FromMilliseconds(200);
            sidePanel.BeginAnimation(Grid.WidthProperty, buttonAnimation);
            /*
            DoubleAnimation fade = new DoubleAnimation();
            fade.From = sidePanel.Opacity;
            fade.To = 0.5;
            fade.Duration = TimeSpan.FromMilliseconds(100);
            sidePanel.BeginAnimation(OpacityProperty, fade);*/
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
            GoToURL("https://github.com/DenisSolicen/MAS-Russifier-NEW");

        }

        private void Android_Click(object sender, RoutedEventArgs e)
        {
            GoToURL("https://discord.gg/4KKTDbtp6z");

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
        #endregion
        void lockUnlockButtons(bool flag)
        {
                DownloadButt.IsEnabled = flag;
                CheckUpdate.IsEnabled = flag;
                Reinstall.IsEnabled = flag;
        }

    }
}
