using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace AZUSA
{



    //進程類
    //IOPortedPrc 是輸出輸入跟 AZUSA 對接的隱藏進程
    class IOPortedPrc
    {
        //名字, 方便向用戶提示用的, 內部如果要識別各引擎應該用 pid
        public string Name;

        //pid 每個進程的惟一的 ID
        public int pid;

        //進程的路徑, 重啟用
        string path;

        //進程的實體
        Process Engine;

        //進程目前的類型
        public PortType currentType = PortType.Unknown;

        //進程負責的接口
        List<string> Ports = new List<string>();

        //不接收廣播
        public bool NoBroadcast = false;

        //記錄意外退出回數
        public int CrashCount = 0;


        //進程可以接管指令
        //RIDs 記錄的是進程接管的指令
        //第二個 bool 記錄的是進程是否只需要指令的參數
        //比如說進程只接管一個指令的話, 指令名其實是不必要的, AZUSA 只傳參數就好, 這樣的話可以把它設成 true
        public Dictionary<string, bool> RIDs = new Dictionary<string, bool>();


        public IOPortedPrc(string name, string enginePath, string arg = "", int Count=0)
        {
            //名字
            Name = name;

            //路徑
            path = enginePath;

            //意外退出回數
            CrashCount = Count;
            

            //這裡是創建進程實體的部分
            //specifies the way the recognizer is run
            Engine = new Process();
            Engine.StartInfo.FileName = path;
            Engine.StartInfo.Arguments = arg;

            //把進程的工作路徑設成進程本身的路徑, 而不是預設的 AZUSA 的路徑
            Engine.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(path);


            //這三行會讓進程被隱藏起來
            //如果不進行隱藏的話, AZUSA 是不能接管其輸入輸出
            //這大概是 .Net 的限制
            Engine.StartInfo.UseShellExecute = false;
            Engine.StartInfo.CreateNoWindow = true;
            Engine.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;



            //接管輸入輸出
            //port I/O to allow communication
            Engine.StartInfo.RedirectStandardOutput = true;
            Engine.StartInfo.RedirectStandardInput = true;

            //當接收到進程的輸出時, 觸發 OutputDataReceived 事件, 由 Engine_OutputDataReceived 處理
            Engine.OutputDataReceived += new DataReceivedEventHandler(Engine_OutputDataReceived);

            //設 EnableRaisingEvents 為 true
            //這樣進程退出時會觸發 Exited 事件, 由 Engine_Exited 處理
            //handling process exit
            Engine.EnableRaisingEvents = true;
            Engine.Exited += new EventHandler(Engine_Exited);

        }

        //返回進程是否已退出
        public bool HasExited()
        {
            try
            {
                return Engine.HasExited;
            }
            catch
            {
                return true;
            }
        }

        //返回進程的輸入端
        public StreamWriter Input
        {
            get
            {
                return Engine.StandardInput;
            }
        }

        //啟動進程
        public void Start()
        {

            Engine.Start();

            //開始聆聽引擎輸出
            Engine.BeginOutputReadLine();

            //引擎輸入設為 AutoFlush 就不用每次寫入都要加一行 flush 了
            Engine.StandardInput.AutoFlush = true;

            //啟動後,取得進程的 ID
            pid = Engine.Id;

        }

        string output = "";

        //啟動進程並取得回傳
        public string StartAndGetOutput()
        {

            Engine.Start();

            //開始聆聽引擎輸出
            Engine.BeginOutputReadLine();

            //引擎輸入設為 AutoFlush 就不用每次寫入都要加一行 flush 了
            Engine.StandardInput.AutoFlush = true;

            //啟動後,取得進程的 ID
            pid = Engine.Id;

            //等待引擎退出
            Engine.WaitForExit();

            return output;

        }

        //暫停處理引擎輸出
        public void Pause()
        {
            try
            {
                Engine.CancelOutputRead();
            }
            catch { }
        }

        //繼續處理引擎輸出
        public void Resume()
        {
            Engine.BeginOutputReadLine();
        }

        //結束進程, 目前只在 AZUSA 退出時使用到, 所以處理得相對簡單些也沒問題
        public void End()
        {

            //首先暫停處理引擎的輸出
            Pause();

            if (Engine != null)
            {
                //移除事件監聽
                Engine.OutputDataReceived -= Engine_OutputDataReceived;
                Engine.Exited -= Engine_Exited;

                //結束引擎
                if (Engine.MainWindowHandle.ToInt32() != 0)
                {
                    Engine.CloseMainWindow();
                }
                if (!Engine.HasExited)
                {
                    Engine.Kill();
                }

                //等待進程順利退出
                Engine.WaitForExit();

                //拋棄進程的實體
                Engine.Dispose();
                Engine = null;
            }

            //退出順利後檢查引擎類型, 再從 ProcessManager 相應的名單中除名
            if (ProcessManager.AIPid.Contains(pid))
            {
                ProcessManager.AIPid.Remove(pid);
            }
            if (ProcessManager.InputPid.Contains(pid))
            {
                ProcessManager.InputPid.Remove(pid);
            }
            if (ProcessManager.OutputPid.Contains(pid))
            {
                ProcessManager.OutputPid.Remove(pid);
            }

            //釋放變量佔用的資源
            Name = null;
            if (RIDs != null)
            {
                RIDs.Clear();
                RIDs = null;
            }

            //然後從 ProcessManager 的進程名單中除名
            ProcessManager.RemoveProcess(this);
        }

        //對進程自行退出的事件處理
        void Engine_Exited(object sender, EventArgs e)
        {
            //首先暫停處理引擎的輸出
            Pause();

            //如果是負責接口的話
            if (Ports.Count != 0)
            {
                foreach (string port in Ports)
                {
                    //取消掉所有接口登錄
                    ProcessManager.Ports.Remove(port);
                }

                //取消掉所有接口登錄
                Ports.Clear();

                //通知其他進程接口有變
                List<IOPortedPrc> ListCopy = new List<IOPortedPrc>(ProcessManager.GetCurrentProcesses());

                foreach (IOPortedPrc prc in ListCopy)
                {
                    prc.Input.WriteLine("PortHasChanged");
                }

                ListCopy = null;
            }

            //移除事件監聽
            Engine.OutputDataReceived -= Engine_OutputDataReceived;
            Engine.Exited -= Engine_Exited;

            //拋棄進程的實體
            Engine.Dispose();
            Engine = null;

            //然後檢查引擎類型, 再從 ProcessManager 相應的名單中除名
            if (ProcessManager.AIPid.Contains(pid))
            {
                ProcessManager.AIPid.Remove(pid);
            }
            if (ProcessManager.InputPid.Contains(pid))
            {
                ProcessManager.InputPid.Remove(pid);
            }
            if (ProcessManager.OutputPid.Contains(pid))
            {
                ProcessManager.OutputPid.Remove(pid);
            }

            //從 ProcessManager 的進程名單中除名
            ProcessManager.RemoveProcess(this);

            //如果是主要引擎的話,嘗試一定次數內重啟
            if (currentType != PortType.Unknown && currentType!=PortType.Application)
            {
                if (CrashCount <= 3)
                {
                    ProcessManager.AddProcess(Name, path, "", CrashCount + 1);

                    ActivityLog.Add(Name + Localization.GetMessage("ENGINERESTART"," has exited unexpectedly. Attempting to restart."));
                }
                else
                {
                    Internals.ERROR(Name + Localization.GetMessage("ENGINEEXIT", " has exited unexpectedly. All restart attempts failed."));
                }
                       
                   
            }

            //釋放變量佔用的資源
            Name = null;
            path = null;
            if (RIDs != null)
            {
                RIDs.Clear();
                RIDs = null;
            }

            //最後檢查完備性, 如果不完備的話發出通知
            if (!ProcessManager.CheckCompleteness())
            {
                Internals.ERROR(Localization.GetMessage("ENGINEMISSING", "Some engines are missing. AZUSA will not execute any MUTAN commands unless AI and I/O are all registered."));
            }
        }

        //處理引擎的輸出
        void Engine_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //如果是空白輸出, 不要理會
            //Ignore NULL and empty inputs that will crash the program
            if (e.Data == null || e.Data.Trim() == "")
            {
                return;
            }

            //activity log
            ActivityLog.Add("From " + Name + ": " + e.Data);

            //如果是詢問, 則調用 MUTAN 表達式解析器, 並返回結東
            //詢問的語法是 "(表達式)?"
            //First check if the engine is asking a question about value of an expression
            if (e.Data.EndsWith("?"))
            {
                //首先保護進程以免受到 BROADCAST 干擾
                NoBroadcast = true;

                string result;

                //去掉最後的問號, 就是表達式了
                //如果格式有誤的話, 會返回 INVALIDEXPR (無效的表達式) 或 IMBALBRACKET (括號不平衡)
                MUTAN.ExprParser.TryParse(e.Data.TrimEnd('?'), out result);
                Engine.StandardInput.WriteLine(result);

                //解除 BROADCAST 干擾保護
                NoBroadcast = false;

                //activity log
                ActivityLog.Add("To " + Name + ": " + result);

                return;
            }

            string RID = "";
            string arg = "";

            //首先假設是溝通用的指令, 主要是用來讓進程宣佈自己的角色和功能, 並取得可用接口等等的溝通協調用的指令  
            //對字串分割並去掉多餘空白
            if (MUTAN.IsExec(e.Data))
            {
                RID = e.Data.Split('(')[0];
                arg = e.Data.Substring(RID.Length + 1, e.Data.Length - RID.Length - 2);
                RID = RID.Trim();
            }


            switch (RID)
            {
                //這是用來進入除錯模式的, 除錯模式下不會要求完備性
                case "DEBUG":
                    Internals.Debugging = true;
                    Variables.Write("$SYS_DEBUG", "TRUE");
                    Internals.MESSAGE(Localization.GetMessage("DEBUG", "Entered debug mode. AZUSA will display all errors and listen to all commands."));
                    return;
                //進行回傳
                case "Return":
                    output = arg;
                    return;
                //這是用來讓進程取得 AZUSA 的 pid, 進程可以利用 pid 檢查 AZUSA 是否存活, 當 AZUSA 意外退出時, 進程可以檢查到並一併退出
                case "GetAzusaPid":

                    //首先保護進程以免受到 BROADCAST 干擾
                    NoBroadcast = true;

                    Engine.StandardInput.WriteLine(Process.GetCurrentProcess().Id);
                    //activity log
                    ActivityLog.Add("To " + Name + ": " + Process.GetCurrentProcess().Id);

                    //解除 BROADCAST 干擾保護
                    NoBroadcast = false;
                    
                    return;
                //這是讓進程宣佈自己的身份的, 這指令應該是進程完成各種初始化之後才用的
                case "RegisterAs":
                    //先記錄現在是否完備
                    bool tmp = ProcessManager.CheckCompleteness();

                    //然後進行相應的登錄
                    switch (arg)
                    {
                        case "AI":
                            currentType = PortType.AI;
                            ProcessManager.AIPid.Add(pid);
                            break;
                        case "Input":
                            currentType = PortType.Input;
                            ProcessManager.InputPid.Add(pid);
                            break;
                        case "Output":
                            currentType = PortType.Output;
                            ProcessManager.OutputPid.Add(pid);
                            break;
                        case "Application":
                            currentType = PortType.Application;
                            break;
                        default:
                            break;
                    }

                    //再次檢查完備性, 如果之前不完備, 現在完備了就進行提示
                    if (!tmp && ProcessManager.CheckCompleteness())
                    {
                        Internals.READY();
                    }

                    return;
                //這是讓進程宣佈自己的可連接的接口, AZUSA 記錄後可以轉告其他進程, 進程之間可以直接對接而不必經 AZUSA
                case "RegisterPort":
                    ProcessManager.Ports.Add(arg.Trim('"'), currentType);
                    this.Ports.Add(arg.Trim('"'));
                    ProcessManager.Broadcast("PortHasChanged");
                    return;
                //這是讓進程取得當前可用所有端口
                case "GetAllPorts":

                    //首先保護進程以免受到 BROADCAST 干擾
                    NoBroadcast = true;

                    string result = "";
                    foreach (KeyValuePair<string, PortType> port in ProcessManager.Ports)
                    {
                        result += port.Key + ",";
                    }

                    Engine.StandardInput.WriteLine(result.Trim(','));

                    //解除 BROADCAST 干擾保護
                    NoBroadcast = false;

                    //activity log
                    ActivityLog.Add("To " + Name + ": " + result.Trim(','));
                    
                    return;
                //這是讓進程取得當前可用的AI 端口(AI引擎的接口)
                case "GetAIPorts":

                    //首先保護進程以免受到 BROADCAST 干擾
                    NoBroadcast = true;

                    result = "";
                    foreach (KeyValuePair<string, PortType> port in ProcessManager.Ports)
                    {
                        if (port.Value == PortType.AI)
                        {

                            result += port.Key + ",";

                        }
                    }

                    Engine.StandardInput.WriteLine(result.Trim(','));

                    //解除 BROADCAST 干擾保護
                    NoBroadcast = false;

                    //activity log
                    ActivityLog.Add("To " + Name + ": " + result.Trim(','));
                    
                    return;
                //這是讓進程取得當前可用的輸入端口(輸入引擎的接口)
                case "GetInputPorts":

                    //首先保護進程以免受到 BROADCAST 干擾
                    NoBroadcast = true;

                    result = "";
                    foreach (KeyValuePair<string, PortType> port in ProcessManager.Ports)
                    {
                        if (port.Value == PortType.Input)
                        {

                            result += port.Key + ",";

                        }
                    }

                    Engine.StandardInput.WriteLine(result.Trim(','));

                    //解除 BROADCAST 干擾保護
                    NoBroadcast = false;

                    //activity log
                    ActivityLog.Add("To " + Name + ": " + result.Trim(','));
                    
                    return;
                //這是讓進程取得當前可用的輸出端口(輸出引擎的接口)
                case "GetOutputPorts":

                    //首先保護進程以免受到 BROADCAST 干擾
                    NoBroadcast = true;
                    
                    result = "";
                    foreach (KeyValuePair<string, PortType> port in ProcessManager.Ports)
                    {
                        if (port.Value == PortType.Output)
                        {

                            result += port.Key + ",";

                        }
                    }

                    Engine.StandardInput.WriteLine(result.Trim(','));

                    //解除 BROADCAST 干擾保護
                    NoBroadcast = false;

                    //activity log
                    ActivityLog.Add("To " + Name + ": " + result.Trim(','));
                    
                    return;
                //這是讓進程可以宣佈自己責負甚麼函式, AZUSA 在接收到這種函件就會轉發給進程
                //函式接管不是唯一的, 可以同時有多個進程接管同一個函式, AZUSA 會每個宣告了接管的進程都轉發一遍
                case "LinkRID":
                    string[] parsed = arg.Split(',');

                    this.RIDs.Add(parsed[0], Convert.ToBoolean(parsed[1]));

                    return;                
                default:
                    break;
            }            

            //檢查整體架構是否完備, 完備或除錯模式下才執行指令
            if (Internals.SysReady || Internals.Debugging)
            {
                //否則假設是 MUTAN 指令,嘗試解析, 如果失敗的話, 就無視掉本次輸出

                //先創建一個可運行物件, 用來儲存解析結果
                MUTAN.IRunnable obj;


                //然後用單行解析器
                if (MUTAN.LineParser.TryParse(e.Data, out obj))
                {
                    //如果成功解析, 則運行物件, 獲取回傳碼
                    MUTAN.ReturnCode tmp = obj.Run();

                    //然後按回傳碼執行指令
                    if (tmp.Command != "")
                    {

                        Internals.Execute(tmp.Command, tmp.Argument);

                    }
                }
            }
            else
            {
                Internals.ERROR(Localization.GetMessage("ENGINEMISSING", "Some engines are missing. AZUSA will not execute any MUTAN commands unless AI and I/O are all registered."));
            }

        }



    }
}
