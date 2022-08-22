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
        public string gitUser;
        public string gitRepo;
        public string browserURL = "";
        public string pathToArchive = "";
        public bool readyToUpdate = false;
        public bool readyToInstall = false;
        public string UpdateVersion = "";
        public string CurrentVersion = "";
        public string UpdateDescription = "";
        public string responseString = "";
        public bool UpdateDescriptionReady;
        public bool debugEnabled = false;

        public string ExeFileName = "";
        public string IgnoreFiles = "";

        public string IniName = "version.ini";

        public int _downloadProcessValue = 0;
        public int _extractProcessValue = 0;
        public int _extractProcessValueMax = 0;


        public async void DownloadUpdate(string gitUsername, string gitRepo)
        {
            Regex regexRegular = new Regex("\".*?.zip\"", RegexOptions.Multiline);
            var matches = regexRegular.Matches(responseString);
            var tempItem = "";
            var fileName = "";

            if (browserURL == "")
            {

                foreach (var item in matches)
                {
                    await Task.Delay(1);
                    if (item.ToString().StartsWith("\"browser"))
                    {
                        tempItem = item.ToString();
                        fileName = item.ToString().Split('/')[item.ToString().Split('/').Length - 1].Replace("\"", "");
                        if(debugEnabled) Debug.WriteLine("File : " + fileName);
                    }
                }

                regexRegular = new Regex("\".*?\"", RegexOptions.Multiline);
                matches = regexRegular.Matches(tempItem);

                foreach (var item in matches)
                {
                    await Task.Delay(1);
                    if (item.ToString().StartsWith("\"https"))
                    {
                        if(debugEnabled) Debug.WriteLine(item);
                        browserURL = item.ToString().Replace("\"", "");
                    }
                }
            }
            string pathArchive = Application.StartupPath + "\\" + fileName;
            if(debugEnabled) Debug.WriteLine($"Path to Archive : {pathArchive}");
            pathToArchive = pathArchive;

            if(debugEnabled) Debug.WriteLine(browserURL);
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
                if(debugEnabled) Debug.WriteLine(ex);
            }


            /*
            // Продолжает попытки скачать файл если выбивает ошибку пути.
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1);
                try
                {
                    if (File.Exists(pathArchive)) { break; }
                    webClient.DownloadFile(browserURL, pathArchive);
                }
                catch
                {
                    await Task.Delay(30000);
                    continue;
                }
            }
            */


            pathToArchive = pathArchive;
            readyToUpdate = true;
        }

        public System.Windows.Controls.ProgressBar solicenBar;
        public void _webClientDownloadChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _downloadProcessValue = e.ProgressPercentage;
            solicenBar.Value = _downloadProcessValue;
            if(debugEnabled) Debug.WriteLine($"Download %{_downloadProcessValue}");
        }

        public async void ExctractArchive(string ExtractPath, bool UpdateInUpdaterExe = false)
        {
            while (_downloadProcessValue != 100)
            {
                if(debugEnabled) Debug.WriteLine("Waiting archive");
                await Task.Delay(10);
            }

            if(debugEnabled) Debug.WriteLine("Extracting archive");
            var path = Application.StartupPath + "\\";

            _extractProcessValueMax = 0;
            _extractProcessValue = 0;

            if (!UpdateInUpdaterExe && ExtractPath != null)
            {
                if (!File.Exists(pathToArchive))
                {
                    if(debugEnabled) Debug.WriteLine("Archive in " + pathToArchive + " not found");
                    return;
                }
                string extractPath = ExtractPath;
                using (ZipFile zip = ZipFile.Read(pathToArchive))
                {
                    _extractProcessValueMax = zip.Entries.Count;

                    try
                    {
                        foreach (ZipEntry e in zip)
                        {
                            if(debugEnabled) Debug.WriteLine(_extractProcessValue);
                            e.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
                            _extractProcessValue += 1;
                        }
                    }
                    catch { }
                    File.WriteAllText(path + IniName, UpdateVersion);
                    if(debugEnabled) Debug.WriteLine("Archive extracted to " + extractPath);


                }
                File.Delete(pathToArchive);
            }
            else
            {
                if(debugEnabled) Debug.WriteLine("Create config");
                CreateConfig();
                if(debugEnabled) Debug.WriteLine($"Create {IniName}");
                var pathTo = Environment.CurrentDirectory + "\\";
                File.WriteAllText(pathTo + IniName, UpdateVersion);
                await Task.Delay(100);
                if(debugEnabled) Debug.WriteLine("Starting Updater");
                Process.Start("Updater.exe");
                if(debugEnabled) Debug.WriteLine("Exiting");
                Environment.Exit(0);
            }
            readyToInstall = true;


        }

        public async Task GetUpdateVersion()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) { return; }
            if (responseString == "") { ResponseStringAsync(); }
            await Task.Delay(1000);

            Regex regexRegular = new Regex("\".*?\"", RegexOptions.Multiline);
            var matches = regexRegular.Matches(responseString);
            var tempItem = "";

            foreach (var item in matches)
            {
                if (item.ToString().StartsWith("\"v"))
                {
                    tempItem = item.ToString().Replace("v", "");
                    if(debugEnabled) Debug.WriteLine(item);
                }
            }

            if(debugEnabled) Debug.WriteLine(tempItem.ToString().Replace("\"", ""), false);
            await Task.Delay(100);
            GetCurrentVersion(tempItem.ToString().Replace("\"", ""), false);
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
                    if (responseString != "") { break; }
                    var resultURL = "https://api.github.com/repos/" + gitUser + "/" + gitRepo + "/releases/latest";
                    responseString = await client.GetStringAsync(resultURL);
                    if(debugEnabled) Debug.WriteLine(responseString);
                    if (responseString != "") { GetUpdateVersion(); }
                }
                catch (Exception e)
                {
                    x++;
                    if(debugEnabled) Debug.WriteLine(e.Message);
                    if(debugEnabled) Debug.WriteLine("Количество попыток:  " + x);
                    continue;
                }
            }


        }

        public async void GetUpdateDescription()
        {
            UpdateDescriptionReady = false;


            Regex regexRegular = new Regex("\".*?\"", RegexOptions.Multiline);
            var matches = regexRegular.Matches(responseString);
            var tempItem = "";
            int x = 1;

            if (responseString == "") { return; }
            if (UpdateVersion == "")
            {
                for (int i = 0; i < x; i++)
                {
                    await Task.Delay(600);
                    if (UpdateVersion != "") { break; }
                    x++;
                }
            }

            foreach (string str2 in responseString.Split(','))
            {
                if (str2 == responseString.Split(',')[responseString.Split(',').Length - 1])
                {
                    string pattern = @"\r\n";
                    if(debugEnabled) Debug.WriteLine(str2.ToString());
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

                    if(debugEnabled) Debug.WriteLine(tempItem);
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
            responseString = "";
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
            DownloadUpdate(gitUser, gitRepo); ExctractArchive(ExtractPath);
        }

        public async void GetCurrentVersion(string updateVersion, bool autoDownloadUpdate)
        {
            var path = Application.StartupPath + "\\";
            if (File.Exists(path + IniName))
            {
                string version = File.ReadAllText(path + IniName);
                CurrentVersion = version;
                if (updateVersion != version)
                {
                    //Если версия обновления не совпадает с текущей версией, то приготовиться к обновлению
                    await Task.Delay(100);
                    if (autoDownloadUpdate) DownloadUpdate(gitUser, gitRepo);

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
                File.WriteAllText(path + IniName, "1.0.0");
                if (autoDownloadUpdate) DownloadUpdate(gitUser, gitRepo);

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

        public async Task CheckUpdate(string GitUsername, string GitRepo)
        {
            if (File.Exists(Application.StartupPath + "\\" + IniName))
                CurrentVersion = File.ReadAllText(Application.StartupPath + "\\" + IniName);

            await Task.Delay(100);
            gitUser = GitUsername;
            gitRepo = GitRepo;

            await GetUpdateVersion();
            await Task.Delay(10000);
            CurrentVersion = File.ReadAllText
                (Application.StartupPath + "\\" + "version.ini");

            if(debugEnabled) Debug.WriteLine("Текущая версия: " + CurrentVersion);
            if(debugEnabled) Debug.WriteLine("Версия обновления: " + UpdateVersion);
            if (UpdateVersion != CurrentVersion && UpdateVersion != "")
            {
                //readyToUpdate = true;
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