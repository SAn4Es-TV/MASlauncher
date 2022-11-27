using System;
using System.IO;
using System.Windows.Forms;

namespace Solicen
{
    class Logger
    {
        public void Start()
        {
            Directory.CreateDirectory(System.Windows.Forms.Application.StartupPath + "\\" + "\\logs\\");
            ClearAndCreateLogsFolder();
            UIExceptionHandlerWinForms.UIException.Start(
                "smtp.gmail.com", 587, "ebzzwqzyawfcuesx", "lilmonix82", "sanes328@gmail.com",
                "lilmonix82@gmail.com", "Ошибка приложения");

            UIExceptionHandlerWinForms.UIException.Log("Программа успешно запущена.");
            UIExceptionHandlerWinForms.UIException.Log("———————————————————————————————————————————————");
        }
        public void Log(string text)
        {
            UIExceptionHandlerWinForms.UIException.Log(text);
        }
        void ClearAndCreateLogsFolder()
        {
            string[] allFiles = Directory.GetFiles(Application.StartupPath + "\\" + "\\logs\\");
            if (allFiles.Length >= 15)
            {
                int i = 0;
                foreach (string str in allFiles)
                {
                    Console.WriteLine(str);
                    if (i == allFiles.Length - 1) { break; }
                    File.Delete(str);
                    i++;
                }
            }
        }
        
    }
}