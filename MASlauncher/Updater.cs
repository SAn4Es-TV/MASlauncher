using System;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Ionic.Zip;

namespace SolicenTEAM
{
    class Updater
    {
        private string _gitUser, _gitRepo, _iniName = "version.ini";
        public string createdAt, publishAt;

        public string browserURL = "", pathToArchive = "", endsWitch = "";

        public bool readyToUpdate = false, readyToInstall = false;
        public bool UpdateDescriptionReady, debugEnabled = true;

        public string UpdateVersion = "", CurrentVersion = "", UpdateDescription = "";     
        public string ExeFileName = "", IgnoreFiles = "";

        public int _downloadProcessValue = 0;
        public int _extractProcessValue = 0;
        public int _extractProcessValueMax = 0;

        private string _response = "";
        private GitFile gitFile;
        public Updater(string gitUser, string gitRepo, string iniName = "version.ini")
        {
            _gitUser = gitUser;
            _gitRepo = gitRepo;
            _iniName = iniName;
        }

        public async void DownloadUpdate()
        {
            if (gitFile == null) return;
            var fileName = gitFile.downloadURL.Split('/')[gitFile.downloadURL.Split('/').Length - 1].Replace("\"", "");
            string pathArchive = Application.StartupPath + "\\" + fileName;
            Debug.WriteLine($"Path to Archive : {pathArchive}");
            if (debugEnabled) Debug.WriteLine($"Path to Archive : {pathArchive}");
            pathToArchive = pathArchive;

            if (debugEnabled) Debug.WriteLine(browserURL);
            if (File.Exists(pathArchive))
                File.Delete(pathArchive);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadProgressChanged += _webClientDownloadChanged;
                    await webClient.DownloadFileTaskAsync(new System.Uri(browserURL), pathArchive);
                }
            }
            catch (Exception ex)
            {
                if (debugEnabled) Debug.WriteLine(ex);
            }

            pathToArchive = pathArchive;
            readyToUpdate = true;
            Debug.WriteLine("readyToUpdate => " + readyToUpdate);
        }

        public System.Windows.Controls.ProgressBar solicenBar;
        public System.Windows.Controls.TextBlock solicenBarText;
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        public void _webClientDownloadChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            solicenBar.Visibility = System.Windows.Visibility.Visible;
            solicenBarText.Visibility = System.Windows.Visibility.Visible;
            _downloadProcessValue = e.ProgressPercentage;
            string downloadSpeed = string.Format("{0} MB/s", (e.BytesReceived / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds).ToString("0.00"));
            solicenBarText.Text = "Загрузка: " + _downloadProcessValue + "%";
            solicenBar.Value = _downloadProcessValue;
            if (debugEnabled) Debug.WriteLine($"Download %{_downloadProcessValue}");
            if (_downloadProcessValue >= 100)
            {
                solicenBar.Visibility = System.Windows.Visibility.Hidden;
                solicenBarText.Visibility = System.Windows.Visibility.Hidden;
                //stopwatch.Reset();
            }
        }

        public async void ExctractArchive(string ExtractPath, bool UpdateInUpdaterExe = false)
        {
            while (_downloadProcessValue != 100)
            {
                if (debugEnabled) Debug.WriteLine("Waiting archive");
                await Task.Delay(10);
            }

            Debug.WriteLine("Extracting archive");
            var path = Application.StartupPath + "\\";

            _extractProcessValueMax = 0;
            _extractProcessValue = 0;

            if (!UpdateInUpdaterExe && ExtractPath != null)
            {
                if (!File.Exists(pathToArchive))
                {
                    Debug.WriteLine("Archive in " + pathToArchive + " not found");
                    return;
                }
                string extractPath = ExtractPath;
                using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(pathToArchive))
                {
                    _extractProcessValueMax = zip.Entries.Count;

                    try
                    {
                        foreach (ZipEntry e in zip)
                        {
                            Debug.WriteLine(_extractProcessValue + "/" + _extractProcessValueMax);
                            e.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
                            _extractProcessValue += 1;
                        }
                    }
                    catch { }
                    File.WriteAllText(path + _iniName, UpdateVersion);
                    Debug.WriteLine("Archive extracted to " + extractPath);


                }
                File.Delete(pathToArchive);
            }
            else
            {
                Debug.WriteLine("Archive for updater");
                Debug.WriteLine("Create config");
                CreateConfig();
                Debug.WriteLine($"Create {_iniName}");
                var pathTo = Environment.CurrentDirectory + "\\";
                File.WriteAllText(pathTo + _iniName, UpdateVersion);
                await Task.Delay(100);
                Debug.WriteLine("Starting Updater");
                Process.Start("Updater.exe");
                Debug.WriteLine("Exiting");
                Environment.Exit(0);
            }
            readyToInstall = true;


        }

        public async Task GetUpdateVersion()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) { return; }
            if (_response == "") { ResponseStringAsync(); }
            await Task.Delay(1000);

            GitFile file = new GitFile(_response);
            if (debugEnabled) Debug.WriteLine(file.version, false);
            await Task.Delay(100);
            GetCurrentVersion(file.version, false);
            await Task.Delay(100);
            if (UpdateDescription == "") { GetUpdateDescription(); }
        }

        HttpClient client = new HttpClient(); //
        async void ResponseStringAsync()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");

            int x = 1;
            for (int i = 0; i < x; i++)
            {
                if (x == 200) { break; }
                try
                {
                    if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) { await Task.Delay(100); continue; }
                    if (_response != "") { break; }
                    var resultURL = $"https://api.github.com/repos/" + $"{this._gitUser}/{this._gitRepo}" + "/releases/latest";
                    _response = await client.GetStringAsync(resultURL);
                    if (debugEnabled) Debug.WriteLine(_response);
                    if (_response != "") { await GetUpdateVersion(); }
                    await Task.Delay(3400);
                }
                catch (Exception e)
                {
                    if (e.Message == "Response status code does not indicate success: 403 (rate limit exceeded).")
                        break;
                    x++;
                    if (debugEnabled) Debug.WriteLine(e.Message);
                    if (debugEnabled) Debug.WriteLine("Количество попыток:  " + x);
                    await Task.Delay(3400);
                    continue;
                }
            }

            if (_response == "") return;
            gitFile = new GitFile(_response);
        }

        public async void GetUpdateDescription()
        {
            UpdateDescriptionReady = false;


            Regex regexRegular = new Regex("\".*?\"", RegexOptions.Multiline);
            var matches = regexRegular.Matches(_response);
            var tempItem = "";
            int x = 1;

            if (_response == "") { return; }
            if (UpdateVersion == "")
            {
                for (int i = 0; i < x; i++)
                {
                    await Task.Delay(600);
                    if (UpdateVersion != "") { break; }
                    x++;
                }
            }

            foreach (string str2 in _response.Split(','))
            {
                if (str2 == _response.Split(',')[_response.Split(',').Length - 1])
                {
                    string pattern = @"\r\n";
                    if (debugEnabled) Debug.WriteLine(str2.ToString());
                    tempItem = str2.ToString().Remove(str2.ToString().Length - 2);
                    tempItem = tempItem.Replace(pattern, "");
                    pattern = @"\";
                    tempItem = tempItem.Replace(pattern, "");
                    tempItem = tempItem.Replace("\"", "");
                    tempItem = tempItem.Substring(3);
                    string[] description = tempItem.Split('*');

                    foreach (string str in description)
                    {
                        var temp = str;
                        if (temp == description[0]) { continue; }
                        temp = str.Replace("*", "");
                        UpdateDescription += " — ";
                        UpdateDescription += temp + "\n\n";
                    }

                    UpdateDescriptionReady = true;

                    if (debugEnabled) Debug.WriteLine(tempItem);
                    break;
                }
            }
            /*
            UpdateDescription += "—————————————————————————————————" + "\n";
            UpdateDescription += "Версия обновления: " + UpdateVersion + "\n";
            UpdateDescription += "—————————————————————————————————" + "\n" + "\n";
            */

            /*
            foreach (var item in matches)
            {
                if (item.ToString().StartsWith("\"#") && (item.ToString().EndsWith(".")))
                {
                    string pattern = @"\r\n";
                    if(debugEnabled) Debug.WriteLine(item.ToString());
                    tempItem = item.ToString().Remove(item.ToString().Length - 2);
                    tempItem = tempItem.Replace(pattern, "");
                    tempItem = tempItem.Substring(3);
                    string[] description = tempItem.Split('*');
                    
                    foreach (string str in description)
                    {
                        var temp = str;
                        if (temp == description[0]) { continue; }
                        temp = str.Replace("*", "");
                        UpdateDescription += " — ";
                        UpdateDescription += temp + "!" + "\n\n";
                    }

                    
                    if(debugEnabled) Debug.WriteLine(tempItem);
                    break;
                }
            }
            */
        }

        public void ReselAll()
        {
            _response = "";
            browserURL = "";
            _downloadProcessValue = 0;
            _extractProcessValue = 0;
            readyToUpdate = false;
            UpdateVersion = "";
            CurrentVersion = "";
        }


        public async void InstallUpdate(string ExtractPath)
        {
            await Task.Delay(10);
            DownloadUpdate(); ExctractArchive(ExtractPath);
        }

        public async void GetCurrentVersion(string updateVersion, bool autoDownloadUpdate)
        {
            var path = Application.StartupPath + "\\";
            if (File.Exists(path + _iniName))
            {
                string version = File.ReadAllText(path + _iniName);
                CurrentVersion = version;
                if (updateVersion != version)
                {
                    //Если версия обновления не совпадает с текущей версией, то приготовиться к обновлению
                    await Task.Delay(100);
                    if (autoDownloadUpdate) DownloadUpdate();

                    readyToUpdate = true;
                    UpdateVersion = updateVersion;
                }
                else if (updateVersion == version)
                {
                    //Если версия обновления совпадает с версией, просто перезаписыть версию обновления
                    UpdateVersion = updateVersion;
                }
            }
            else
            {
                //Если файла не существует, создает его и записывает нулевую версию программы
                File.WriteAllText(path + _iniName, "1.0.0");
                if (autoDownloadUpdate) DownloadUpdate();

            }
        }

        public void CreateConfig()
        {
            if (ExeFileName != "")
            {
                var allInfo = ExeFileName + "\n" + IgnoreFiles;
                File.WriteAllText(Environment.CurrentDirectory + "\\" + "UpdateConfig.ini", allInfo);
            }
        }

        public async Task CheckUpdate()
        {
            if (File.Exists(Application.StartupPath + "\\" + _iniName))
                CurrentVersion = File.ReadAllText(Application.StartupPath + "\\" + _iniName);

            await Task.Delay(100);
            await GetUpdateVersion();
            await Task.Delay(10000);
            CurrentVersion = File.ReadAllText
                (Application.StartupPath + "\\" + "version.ini");

            if (debugEnabled) Debug.WriteLine("Текущая версия: " + CurrentVersion);
            if (debugEnabled) Debug.WriteLine("Версия обновления: " + UpdateVersion);
            if (UpdateVersion != CurrentVersion && UpdateVersion != "")
            {
                //readyToUpdate = true;
            }

        }
    }

    class GitFile
    {
        public string downloadURL = "";
        public string name;
        public string version;
        public string createdAt;
        public string publishedAt;

        public GitFile(string response)
        {
            Get(response);
        }

        public void Get(string response)
        {
            Regex regex = new Regex("(\".*?\":.*)(,)?", RegexOptions.Multiline);
            var collection = regex.Matches(response);

            try
            {
                name = Solicen.EX.RegexHelper.MatchToString(collection, "\"name\"").Split(':')[1].Trim(' ').Trim('\"');
                downloadURL = Solicen.EX.RegexHelper.MatchToString(collection, "browser_download_url").Split(':')[1].Trim(' ').Trim('\"');
                version = Solicen.EX.RegexHelper.MatchToString(collection, "tag_name").Split(':')[1].Trim(' ').Trim('\"');
                createdAt = Solicen.EX.RegexHelper.MatchToString(collection, "created_at").Split(':')[1].Trim(' ').Trim('\"');
                publishedAt = Solicen.EX.RegexHelper.MatchToString(collection, "published_at").Split(':')[1].Trim(' ').Trim('\"');
            }
            catch
            {
                // Возникла ошибка при парсинге Гита.
                return;
            }
        }
    }

    public class GitDesc
    {
        public string gitURL = "";
        public string resultString = "";
        public void GetGitHubDesc()
        {
            WebClient web = new WebClient();
            web.Proxy = new WebProxy();
            resultString = web.DownloadString(gitURL);
        }
    }
}