using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace AZUSA
{
    //負責提取不同語言的系統提示, 用戶可以在 LANG 自定義語言和相應的信息內容
    
    static class Localization
    {
        public static string CurrentLanguage = "EN";

        public static void Initialize()
        {
            Variables.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\LANG");

            if (Variables.Exist("SYS_LANG"))
            {
                CurrentLanguage = Variables.Read("SYS_LANG");
            }
        }

        public static string GetMessage(string ID, string Default, string arg="")
        {
            if (Variables.Exist("$SYS_" + CurrentLanguage + "_" + ID))
            {
                return Variables.Read("$SYS_" + CurrentLanguage + "_" + ID).Replace("{arg}",arg).Trim('"');
            }
            else
            {
                return Default.Replace("{arg}", arg);
            }
        }
    }
}
