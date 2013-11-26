using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace AZUSA
{
    static class Variables
    {
        //用來儲存變量名稱(ID)和值
        static Dictionary<string, string> storage = new Dictionary<string, string>();

        //系統內建提供了一組日期時間的動態變量
        static string[] DateTimeVars = new string[] { "Y", "M", "D", "h", "m", "s", "d" };

        //線程鎖, 在多線程環境下保護好變量
        static private object MUTEX= new object();
        
        //從檔案讀取變量
        static public void Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                //用來記錄當前行數
                int numLine = 1;

                //暫存當前這行的內容
                string[] entry;

                //暫存解析出來的 ID
                string ID;

                //逐行讀取
                foreach (string line in File.ReadAllLines(filePath))
                {
                    try
                    {
                        //如果是空白行或注解就跳過, 否則進行解析
                        if (line.Trim() != "" && !line.StartsWith("#"))
                        {
                            entry = line.Trim().Split('=');
                            ID = entry[0];

                            //寫入變量環境
                            Write(ID.Trim(), line.Replace(ID + "=", "").Trim());
                        }
                    }
                    catch
                    {
                        //如果解析失敗代表格式有問題
                        Internals.ERROR("Ill-formatted data in line " + numLine.ToString() + "of " + filePath);
                    }

                    //更新當前行數
                    numLine++;
                }
            }
        }

        //保存變量到檔案
        static public void Save(string filePath)
        {
            //用來暫存已經更新的變量名稱
            List<string> updated = new List<string>();

            //用來暫存要寫入檔案的內容
            List<string> newConfig = new List<string>();

            //如果檔案已經存在, 要先讀取一下已有的檔案, 把有誤的內容更新, 把原來的其他內容保存
            if (File.Exists(filePath))
            {
               
                //暫存解析結果
                string[] parsed;
                string ID;
                string val;

                //首先把檔案原本的內容解析
                //update old values in file
                foreach (string line in File.ReadAllLines(filePath))
                {
                    //從等號分割
                    parsed = line.Split('=');

                    //如果有多或等於兩部分, 表示是定義變數的值的 (多於是因為值裡面可以有等號出現)
                    //see if the line is defining a variable
                    if (parsed.Length >= 2)
                    {
                        //提取 ID 和值
                        ID = parsed[0];
                        val = line.Replace(ID + "=", "");
                        ID = ID.Trim();

                        //然後檢查這個變量在當前的環境是否存在, 如果是的話, 用當前的值替換
                        //see if the variable already exists
                        if (storage.ContainsKey(ID))
                        {
                            newConfig.Add(ID + "=" + storage[ID]);
                        }
                        //否則就保持原狀
                        else //keep it
                        {
                            newConfig.Add(line);
                        }

                        //加入已更新的變量
                        updated.Add(ID);
                    }
                    //如果是其他的東西就繼續保留
                    else
                    {
                        newConfig.Add(line);
                    }
                }

            }

            //然後把原來的檔案裡沒有的變量也加進去
            //add new entries that are not in the file
            foreach (KeyValuePair<string, string> pair in storage)
            {
                //如果變量名不是以 $ 開頭, 以及變量還沒更新
                if (!pair.Key.StartsWith("$") && !updated.Contains(pair.Key))
                {
                    //就把它加進寫入的內容
                    newConfig.Add(pair.Key + "=" + pair.Value);
                }
            }

            //最後以 UTF8 編碼寫入
            File.WriteAllLines(filePath, newConfig.ToArray(),Encoding.UTF8);

        }

        //寫入變量
        static public void Write(string name, string val)
        {
            //先鎖好環境
            lock (MUTEX)
            {
                //不能寫入內建的日期時間變量
                //cannot write to date time variables
                if (DateTimeVars.Contains(name))
                {
                    return;
                }

                //如果已經有這變量, 就更新值, 否則就添加
                if (storage.ContainsKey(name))
                {
                    storage[name] = val;
                }
                else
                {
                    storage.Add(name, val);
                }
            }
        }

        //檢查變量是否存在
        static public bool Exist(string name)
        {
            //如是日期時間變量就返回 true
            //interrupt for date time variables
            if (DateTimeVars.Contains(name)) { return true; }
            //否則就返回是否環境是否存在這變量
            return storage.ContainsKey(name);
        }

        //讀取變量
        static public string Read(string name)
        {
            //如果讀取的是日期時間的話就返回相應的值, 否則就返回變量環境裡的值
            //interrupt with date time variables
            switch (name)
            {
                case "Y":
                    return DateTime.Now.Year.ToString();

                case "M":
                    return DateTime.Now.Month.ToString();

                case "D":
                    return DateTime.Now.Day.ToString();

                case "h":
                    return DateTime.Now.Hour.ToString();

                case "m":
                    return DateTime.Now.Minute.ToString();

                case "s":
                    return DateTime.Now.Second.ToString();

                case "d":
                    return DateTime.Now.DayOfWeek.ToString();

                default:                    
                    return storage[name];                    
            }


        }
    }
}
