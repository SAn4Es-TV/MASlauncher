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
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Toolkit.Uwp.Notifications;

using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Windows.UI.Xaml.Shapes;
using Solicen;
using NLog.Fluent;

namespace MASlauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private static string StartupPath = System.Windows.Forms.Application.StartupPath;

        public string settingsPath = StartupPath + @"\Settings.txt";
        public string PathToSetup = StartupPath + @"\Setup.exe";
        public string PathToTranslate = StartupPath + @"\";
        public string PathToMASFolder = "";
        public string PathToMASExe = "";
        public string PathToPersistent = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\RenPy\Monika After Story";
        public string PathToPersistentBackup = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\RenPy\MAS-Backup";
        public string downloadString = "";
        public string settingsString = "";
        public string[] buttonText = { "Установить", "Обновить", "Начать игру" };
        public int type = 2;
        public bool settingsFlag = false;
        public bool handHideFlag = false;
        public bool secondErrorShow = false;
        public string oldErrorText = "";
        DirectoryInfo MASpath;
        FileInfo exePath; 
        public static bool IsNight => DateTime.Now.Hour > 22 ||
                                        DateTime.Now.Hour < 6;               // Проверка День/Ночь

        string CurrentVersion;
        string MASCurrentVersion;

        public DoubleAnimation _start;  // Анимация запуска
        public DoubleAnimation _quit;   // Анимация выхода

        SolicenTEAM.Updater launcherUpdater = new SolicenTEAM.Updater("SAn4Es-TV", "MASlauncher");
        SolicenTEAM.Updater translateUpdater = new SolicenTEAM.Updater("DenisSolicen", "MAS-Russifier-NEW", "translate.ini");
        SolicenTEAM.Updater masUpdater = new SolicenTEAM.Updater("Monika-After-Story", "MonikaModDev", "mas.ini");

        string ddlcLink = "https://drive.google.com/u/0/uc?id=1o6urVBdzI1K8KY_z_VrhaYqZZ-dUfZnq&export=download&confirm=t&uuid=108a5d75-09cd-422a-ac88-4ee057586531";

        Logger logger = new Logger();
        bool loggerRun = true;

        public MainWindow()
        {
            InitializeComponent();
            //ImageSource ico = new BitmapImage(new Uri("pack://application:,,,/menu_new.png"));
            //this.Icon = ico;
            if (loggerRun) { logger.Start(); }
            initializeImages();
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

            masUpdater.endsWitch = "Mod.zip\"";

            //CheckTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);

            //DownloadButt.Content = buttonText[type];

            DownloadProgress.Value = 0;
            DownloadProgress.Visibility = Visibility.Hidden;
            //DownloadString.Content = downloadString;
            // DownloadButt.Content = buttonText[type];
            translateUpdater.solicenBar = DownloadProgress;
            translateUpdater.solicenBarText = DownloadData;

            launcherUpdater.solicenBar = DownloadProgress;
            launcherUpdater.solicenBarText = DownloadData;
            launcherUpdater.ExeFileName = "MASlauncher";
            PersistentPath.Text = PathToPersistent;
            PathToDir.Text = PathToMASFolder;
            Task.Factory.StartNew(async () =>
            {
                    await launcherUpdater.CheckUpdate();
                this.Dispatcher.Invoke(() =>
                {
                    ver.Text = "Версия клиента: " + launcherUpdater.CurrentVersion.ToString();
                    logger.Log("Проверка обновлений клиента завершена");
                });
            });

            UpdateLauncherAsync();

            _ = Task.Run(async () =>
            {
                while (true)
                {

                    await CheckTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);
                    Task.Delay(300000).Wait();
                }
            });
            Timer timer = new Timer();
            timer.Interval = 5000;
            timer.Tick += Timer_Tick;
            timer.Start();

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("CustomIconWindows.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    if (WindowState != System.Windows.WindowState.Minimized)
                    {
                        this.Hide();
                        this.WindowState = WindowState.Minimized;
                        handHideFlag = true;
                    }
                    else
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                    }
                };
            if (!String.IsNullOrEmpty(PathToMASFolder))
            {
                FileSystemWatcher watcher = new FileSystemWatcher(PathToMASFolder);
                watcher.Changed += delegate(object sender, FileSystemEventArgs e)
                {
                    if (e.ChangeType != WatcherChangeTypes.Changed)
                    {
                        return;
                    }
                    if (e.FullPath.EndsWith("errors.txt"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Stream myStream;
                            using (myStream = File.Open(e.FullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                            {
                                StreamReader reader = new StreamReader(myStream);
                                string text = reader.ReadToEnd();
                                if (oldErrorText != oldErrorText || oldErrorText == "")
                                {
                                    Error error = new Error();
                                    error.Show();
                                    logger.Log("Произошла ошибка при запуске игры");
                                }
                                oldErrorText = text;
                            }
                        });

                        send(PathToMASFolder + "/errors.txt", PathToMASFolder + "/traceback.txt", PathToMASFolder + "/log.txt");
                    }
                    if (e.FullPath.EndsWith("traceback.txt"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Stream myStream;
                            using (myStream = File.Open(e.FullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                            {
                                StreamReader reader = new StreamReader(myStream);
                                string text = reader.ReadToEnd();
                                if (oldErrorText != oldErrorText || oldErrorText == "")
                                {
                                    Error error = new Error();
                                    error.Show();
                                    logger.Log("Произошла ошибка при запуске игры");
                                }
                                oldErrorText = text;
                            }
                        });

                        send(PathToMASFolder + "/errors.txt", PathToMASFolder + "/traceback.txt", PathToMASFolder + "/log.txt");
                    }
                };
                watcher.Created += delegate (object sender, FileSystemEventArgs e)
                {
                    if (e.FullPath.EndsWith("errors.txt"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Stream myStream;
                            using (myStream = File.Open(e.FullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                            {
                                StreamReader reader = new StreamReader(myStream);
                                string text = reader.ReadToEnd();
                                if (oldErrorText != oldErrorText || oldErrorText == "")
                                {
                                    Error error = new Error();
                                    error.Show();
                                    logger.Log("Произошла ошибка при запуске игры");
                                }
                                oldErrorText = text;
                            }
                        });
                        send(PathToMASFolder + "/errors.txt", PathToMASFolder + "/traceback.txt", PathToMASFolder + "/log.txt");
                        //send(PathToMASFolder + "/errors.txt", PathToMASFolder + "/traceback.txt");
                    }
                    if (e.FullPath.EndsWith("traceback.txt"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Stream myStream;
                            using (myStream = File.Open(e.FullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                            {
                                StreamReader reader = new StreamReader(myStream);
                                string text = reader.ReadToEnd();
                                if (oldErrorText != oldErrorText || oldErrorText == "")
                                {
                                    Error error = new Error();
                                    error.Show();
                                    logger.Log("Произошла ошибка при запуске игры");
                                }
                                oldErrorText = text;
                            }
                        });
                        send(PathToMASFolder + "/errors.txt", PathToMASFolder + "/traceback.txt", PathToMASFolder + "/log.txt");
                        //send(PathToMASFolder + "/errors.txt", PathToMASFolder + "/traceback.txt");
                    }
                };
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
            }
            //if(File.Exists(PathToMASFolder + "/errors.txt"))
                //send(PathToMASFolder + "/errors.txt", PathToMASFolder + "/traceback.txt");
        }
        void initializeImages()
        {
            //if (!IsNight)
            try
            {
                ___Monika_png.Source = new BitmapImage(new Uri(StartupPath + "/Assets/monikaroomdaylight.jpg"));
            }
            catch
            {
                logger.Log("Ошибка с дневным фоном!");
            }
            try
            {
                ___Monika_png.Source = new BitmapImage(new Uri(StartupPath + "/Assets/monikaroomnight.png"));
            }
            catch
            {
                logger.Log("Ошибка с ночным фоном!");
            }
            //else
            logo.Source = new BitmapImage(new Uri(StartupPath + "/Assets/rus_logo_mas.png"));
            img1.Source = new BitmapImage(new Uri(StartupPath + "/Assets/4945914.png"));
            img2.Source = new BitmapImage(new Uri(StartupPath + "/Assets/1660165.png"));
            img3.Source = new BitmapImage(new Uri(StartupPath + "/Assets/4926624.png"));
            img4.Source = new BitmapImage(new Uri(StartupPath + "/Assets/4926624.png"));
            img5.Source = new BitmapImage(new Uri(StartupPath + "/Assets/2639683.png"));
            img6.Source = new BitmapImage(new Uri(StartupPath + "/Assets/151776.png"));

        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            var process = Process.GetProcesses().FirstOrDefault(p => p.ProcessName == "Setup");
            var ddlcProcess = Process.GetProcesses().FirstOrDefault(p => p.ProcessName == "DDLC");
            if (process == null)
                lockUnlockButtons(true);
            else
                lockUnlockButtons(false);

            if (ddlcProcess == null && !handHideFlag)
            {
                if(this.Visibility == Visibility.Hidden)
                    this.Show();
                this.WindowState = WindowState.Normal;
            }
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
                        //DownloadTranslate();
                        type = 1;
                        logger.Log("Папка MAS выбрана");
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
            if (File.Exists(StartupPath + "\\Setup.bin"))
                File.Move(StartupPath + "\\Setup.bin", StartupPath + "\\Setup.bak");
            DownloadTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);
            while (!translateUpdater.readyToInstall) await Task.Delay(100);
            UpdateTranslate();
            lockUnlockButtons(true);
        }
        // Запустить мод
        void RunGame()
        {

            logger.Log("Запуск игры");
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = PathToMASExe;
            proc.UseShellExecute = true;
            proc.Verb = "runas";

            Process.Start(proc); // запускаем программу
            handHideFlag = false;
            this.Hide();

            /*if((bool)CloseWhenStart.IsChecked)
                this.Close();*/
        }
        // Обновить перевод
        void UpdateTranslate()
        {
            logger.Log("Запуск переводчика");
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = PathToSetup;
            proc.UseShellExecute = true;
            proc.Arguments = $" -offshort -silent -PATH:\"{PathToMASExe}\"";
            proc.Verb = "runas";

            Process.Start(proc); // запускаем программу
            type = 2;
            DownloadButt.Content = buttonText[type];
        }

        async Task CheckTranslateUpdate(string user, string repo, string path = "")
        {
            Debug.WriteLine("-= Checking Translate Updates =-");
            logger.Log("Проверяю обновления перевода");
            if (File.Exists(StartupPath + "\\" + "translate.ini"))
                CurrentVersion = File.ReadAllText(StartupPath + "\\" + "translate.ini");

            await Task.Delay(10);

            

            await translateUpdater.GetUpdateVersion();
            await Task.Delay(10);
            CurrentVersion = File.ReadAllText(StartupPath + "\\" + "translate.ini");

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
            logger.Log("Проверка обновлений перевода завершена!");
        }
        async Task DownloadTranslateUpdate(string user, string repo, string path = "")
        {
            Debug.WriteLine("-= Downloading Translate =-");
            logger.Log("Загружаю перевод");
            if (File.Exists(StartupPath + "\\" + "translate.ini"))
                CurrentVersion = File.ReadAllText(StartupPath + "\\" + "translate.ini");

            await Task.Delay(10);

            
            await translateUpdater.GetUpdateVersion();
            await Task.Delay(10);
            CurrentVersion = File.ReadAllText(StartupPath + "\\" + "translate.ini");

            Debug.WriteLine("This translate version: " + CurrentVersion);
            Debug.WriteLine("New translate version: " + translateUpdater.UpdateVersion);
            if (translateUpdater.UpdateVersion != CurrentVersion && translateUpdater.UpdateVersion != "")
            {
                while (!translateUpdater.UpdateDescriptionReady) await Task.Delay(100);
                string s = translateUpdater.UpdateDescription;
                translateUpdater.solicenBar = DownloadProgress;
                translateUpdater.DownloadUpdate();
                translateUpdater.ExctractArchive(path);
            }
            logger.Log("Перевод успешно загружен!");
        }

        // Сохранить настройки
        void SaveSettings()
        {
            settingsString = MASpath.FullName + '\n';
            //CloseWhenStart.IsChecked + '\n';
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
                    /*if (data[1] != null && data[1] != "\r")
                        CloseWhenStart.IsChecked = bool.Parse(data[1]);*/

                    PathToDir.Text = PathToMASFolder;
                    logger.Log("Настройки успешно загружены");
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
            UpdateLauncherAsync(true);

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
            string pathToArchive = StartupPath + "\\ddlc-win.zip";
            if (File.Exists(StartupPath + "\\ddlc-win.zip"))
            {
                // Дополнительная проверка на соотвествие архива ddlc-win.zip на оригинальность. //by Solicen
                if (!Solicen.EX.Zip.FileExist(pathToArchive, "fonts.rpa") && !Solicen.EX.Zip.FileExist(pathToArchive, "firstrun"))
                {
                    System.Windows.MessageBox.Show("Обнаружен не оригинальный архив ddlc-win.zip, переустановка невозможна." +
                    "\nПожалуйста скачайте официальную копию DDLC с сайта ddlc.moe (не Steam версию), и поместите в папку лаунчера согласившись на замену, для работы переустановки игры через лаунчер." +
                    "Благодарим за понимание.", "Monika After Story");
                    return;
                }

                if (File.Exists(PathToMASFolder + "\\game\\script-topics.rpyc"))
                {              
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
                        using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(pathToArchive))
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


                        if (File.Exists(StartupPath + "\\" + "mas.ini"))
                            MASCurrentVersion = File.ReadAllText(StartupPath + "\\" + "mas.ini");

                        await Task.Delay(10);

                        
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
                        masUpdater.DownloadUpdate();
                        masUpdater.ExctractArchive(PathToMASFolder + "\\game");
                        if (File.Exists(StartupPath + "\\translate.ini")) 
                            File.Delete(StartupPath + "\\translate.ini");

                        while (!masUpdater.readyToInstall) await Task.Delay(100);
                        if ((bool)TranslateWhenReinstall.IsChecked)
                        {
                            await DownloadTranslateUpdate("DenisSolicen", "MAS-Russifier-NEW", PathToTranslate);

                            while (!translateUpdater.readyToInstall) await Task.Delay(100);
                            UpdateTranslate();
                        }
                        lockUnlockButtons(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ERROR: " + ex);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Для полноценной переустановки нам требуется наличие файлов Monika-After-Story в папке с игрой, пожалуйста скачайте Monika-After-Story с их сайта, и поместите в папку game внутри папки с игрой для работы переустановки игры через лаунчер.\n" +
                "Благодарим за понимание.", "Monika After Story");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Для полноценной переустановки нам требуется наличие архива ddlc-win.zip в папке с лаунчером, пожалуйста скачайте официальную копию DDLC с их сайта, и поместите в папку лаунчера для работы переустановки игры через лаунчер.\n" +
                "Благодарим за понимание.", "Monika After Story");
            }
        }

        public async Task UpdateLauncherAsync(bool showMessage = false)
        {
            await launcherUpdater.CheckUpdate();
            Debug.WriteLine("-= Checking launcher Updates =-");
            Debug.WriteLine("This launcher version: " + launcherUpdater.CurrentVersion);
            Debug.WriteLine("New launcher version: " + launcherUpdater.UpdateVersion);
            if (launcherUpdater.UpdateVersion == launcherUpdater.CurrentVersion && launcherUpdater.UpdateVersion != "")
            {
                if(showMessage)
                    System.Windows.Forms.MessageBox.Show("Установлена новейшая версия програмного обеспечения!");

            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Вышло обновление лаунчера");
                if (launcherUpdater.UpdateVersion != "")
                { await launcherUpdater.CheckUpdate(); }

                launcherUpdater.DownloadUpdate();

                while (!launcherUpdater.readyToUpdate)
                {
                    await Task.Delay(100);
                    Debug.WriteLine(launcherUpdater.readyToUpdate);
                }

                Debug.WriteLine("-= Extract Archive =-");
                launcherUpdater.ExctractArchive(StartupPath + "\\", true);
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
                SaveSettings();
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
            Debug.WriteLine(flag);
                DownloadButt.IsEnabled = flag;
                CheckUpdate.IsEnabled = flag;
                Reinstall.IsEnabled = flag;
        }

        void send(string pathToTraceback, string pathToError, string pathToLog)
        {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            var plainTextBytes = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName));
            string themeName = "Ошибка приложения | UID:" + plainTextBytes;

            // Создаем новый экземпляр настроек почты, работающий независимо от логгера
            var email = UIExceptionHandlerWinForms.UIException.Email(
                "smtp.gmail.com", 587, "ebzzwqzyawfcuesx", "lilmonix82", "sanes328@gmail.com",
                "lilmonix82@gmail.com", "Ошибка приложения"); 
            var email1 = UIExceptionHandlerWinForms.UIException.Email(
                "smtp.gmail.com", 587, "ebzzwqzyawfcuesx", "lilmonix82", "solicenteam@gmail.com",
                "lilmonix82@gmail.com", "Ошибка приложения");

            // Добавляем к письму файл traceback или лог от MAS, можно добавить сколько угодно файлов.
            if (File.Exists(pathToTraceback))
                UIExceptionHandlerWinForms.UIException.AttachFile(pathToTraceback);
            if (File.Exists(pathToError))
                UIExceptionHandlerWinForms.UIException.AttachFile(pathToError);
            //if (File.Exists(pathToLog))
            //    UIExceptionHandlerWinForms.UIException.AttachFile(pathToLog);

            // Еще можем сделать так, то есть добавить скриншот открытого приложения MAS с ошибкой если в traceback или логе будет что-то не понятно, у нас будет дополнительная информация, ничего лишнего захватить не должен
            //UIExceptionHandlerWinForms.UIException.ScreenshotWindow("DDLC");

            //var log = UIExceptionHandlerWinForms.UIException.GetLastFileLogPath();
            //UIExceptionHandlerWinForms.UIException.AttachLog(log);

            // Отправляем письмо
            UIExceptionHandlerWinForms.UIException.SendMail(email, "UID: " + plainTextBytes + ". \nДержи файлы с ошибками", themeName);

            // Добавляем к письму файл traceback или лог от MAS, можно добавить сколько угодно файлов.
            if (File.Exists(pathToTraceback))
                UIExceptionHandlerWinForms.UIException.AttachFile(pathToTraceback);
            if (File.Exists(pathToError))
                UIExceptionHandlerWinForms.UIException.AttachFile(pathToError);

            UIExceptionHandlerWinForms.UIException.SendMail(email1, "UID: " + plainTextBytes + ". \nДержи файлы с ошибками", themeName);
        }

        private void CloseWhenStart_Checked(object sender, RoutedEventArgs e)
        {
        }
    }
}
