using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace AZUSA
{
    static class Variables
    {
        //用來儲存變量名稱(ID)和值
        static Dictionary<string, string> storage = new Dictionary<string, string>();

        //線程鎖, 在多線程環境下保護好變量
        static private object variableMUTEX = new object();

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
                        Internals.ERROR(Localization.GetMessage("ILLFORMAT", "Ill-formatted data in line {arg}", numLine.ToString()) + " [" + filePath + "]");
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
                        //而且變量不是以 $ 開頭
                        //see if the variable already exists
                        if (storage.ContainsKey(ID) && !ID.StartsWith("$"))
                        {
                            newConfig.Add(ID + "=" + storage[ID]);
                        }
                        //以 _$ 開頭就是設預設值的
                        else if (storage.ContainsKey("_" + ID) && ID.StartsWith("$"))
                        {
                            newConfig.Add(ID + "=" + storage["_" + ID]);
                            updated.Add("_" + ID);
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

                    //如果是設預設值的話先去掉"_"
                    if (pair.Key.StartsWith("_$"))
                    {
                        newConfig.Add(pair.Key.TrimStart('_') + "=" + pair.Value);
                    }
                    //否則就正常寫入就行了
                    else
                    {
                        newConfig.Add(pair.Key + "=" + pair.Value);
                    }
                }
            }

            //最後以 UTF8 編碼寫入
            File.WriteAllLines(filePath, newConfig.ToArray(), Encoding.UTF8);

        }

        //寫入變量
        static public void Write(string name, string val)
        {
            //先鎖好環境
            lock (variableMUTEX)
            {
                //不能寫入內建的特殊變量
                if (name.StartsWith("{") && name.EndsWith("}"))
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

                //activity log
                ActivityLog.Add(Localization.GetMessage("VALCHANGE", "Value of " + name + " has been changed to ", name) + val);

            }
        }

        //檢查變量是否存在
        static public bool Exist(string name)
        {
            //如是特殊變量, 且不含加號(這是為了避免 {%h} + : + {%m} 被理解成 { %h}+:+{%m } 就返回 true
            //interrupt for date time variables
            if (name.StartsWith("{") && name.EndsWith("}") && !name.Contains('+')) { return true; }
            //否則就返回是否環境是否存在這變量
            return storage.ContainsKey(name);
        }

        //讀取變量
        static public string Read(string name)
        {
            //字串內出現引號會導致解析錯誤, 定義特殊變量
            if (name == "{\"}" || name == "{QUOT}")
            {
                return "\"";
            }
            //字串內出現分號會導致解析錯誤, 定義特殊變量
            else if (name == "{.,}")
            {
                return ";";
            }
            // AZUSA 執行目錄
            else if (name == "{AZUSA}")
            {
                return Directory.GetCurrentDirectory();
            }
            //簡單的 HTTP GET 回傳
            else if (name.StartsWith("{http"))
            {
                string url = name.Substring(1, name.Length - 2);

                WebRequest req = WebRequest.Create(url);
                using (StreamReader sw = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    return sw.ReadToEnd();
                }

            }
            //函式回傳
            else if (name.StartsWith("{") && name.EndsWith("}") && name.Contains("("))
            {
                name = name.Trim('{', '}');
                string cmd = name.Split('(')[0];
                string arg = name.Remove(name.Length - 1).Replace(cmd + "(", "");

                //檢查是否有引擎登記接管了這個指令
                // routed 記錄指令是否已被接管
                bool routed = false;

                List<IOPortedPrc> ListCopy = new List<IOPortedPrc>(ProcessManager.GetCurrentProcesses());

                //檢查每一個現在運行中的進程
                foreach (IOPortedPrc prc in ListCopy)
                {
                    try
                    {
                        //如果進程有接管這個指令, 就把指令內容傳過去
                        if (prc.RIDs.ContainsKey(cmd))
                        {
                            //設 routed 為 true
                            routed = true;

                            //根據 RIDs 的值,決定只傳參數還是指令跟參數整個傳過去
                            //RIDs 的值如果是 true 的話就表示只傳參數
                            if (prc.RIDs[cmd])
                            {
                                prc.WriteLine(arg);
                            }
                            else
                            {
                                prc.WriteLine(cmd + "(" + arg + ")");
                            }

                            return prc.GetReturn();
                        }
                    }
                    catch { }
                }

                //扔掉 ListCopy
                ListCopy = null;

                if (!routed)
                {
                    if (File.Exists(Environment.CurrentDirectory + @"\Routines\" + cmd + ".exe"))
                    {
                        IOPortedPrc prc = new IOPortedPrc(name, Environment.CurrentDirectory + @"\Routines\" + cmd + ".exe", arg);
                        return prc.StartAndGetOutput();
                    }
                    else if (File.Exists(Environment.CurrentDirectory + @"\Routines\" + cmd + ".bat"))
                    {
                        IOPortedPrc prc = new IOPortedPrc(cmd, "cmd.exe", "/C \"" + Environment.CurrentDirectory + @"\Routines\" + cmd + ".bat\" " + arg);
                        return prc.StartAndGetOutput();

                    }
                    else if (File.Exists(Environment.CurrentDirectory + @"\Routines\" + cmd + ".vbs"))
                    {
                        IOPortedPrc prc = new IOPortedPrc(cmd, "cscript.exe", " \"" + Environment.CurrentDirectory + @"\Routines\" + cmd + ".vbs\" " + arg);
                        return prc.StartAndGetOutput();
                    }
                }

                return name;
            }
            //日期時間
            else if (name.StartsWith("{") && name.EndsWith("}"))
            {
                string datetime = DateTime.Now.ToString(name.TrimStart('{').TrimEnd('}'));
                if (datetime != name.TrimStart('{').TrimEnd('}'))
                {
                    return datetime;
                }
                else
                {
                    return name;
                }

            }
            //否則就返回變量環境裡的值
            else
            {
                return storage[name];
            }
        }
    }
}
