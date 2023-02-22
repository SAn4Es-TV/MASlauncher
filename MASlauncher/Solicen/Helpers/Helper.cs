using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Solicen.EX
{
    public static class Zip
    {
        public static bool FileExist(this Ionic.Zip.ZipFile zip, string filename)
        {
            if (zip.EntryFileNames.Cast<string>().Any(x => x.Contains(filename)))
                return true;
            else
                return false;
        }

        public static bool FileExist(string pathToArchive, string filename)
        {
            using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(pathToArchive))
            {
                if (zip.EntryFileNames.Cast<string>().Any(x => x.Contains(filename)))
                    return true;
                else
                    return false;
            }
        }
    }

    public class RegexHelper
    {
        public static string StringToValue(string line)
        {
            var item = Regex.Split(line, ":");
            return item[1].Trim(' ').Trim('\"');
        }

        public static string MatchToString(MatchCollection collection, string contains = "")
        {
            var i = collection.Cast<string>().FirstOrDefault(x => x.Contains(contains));
            if (i == null)
                return "NULL";
            else
                return i.TrimEnd(',');

        }
    }


}
