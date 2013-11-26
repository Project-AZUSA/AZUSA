using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace AZUSA
{
    //進程類型
    enum ProcessType { Input, Output, AI, Routine }


    //進程類
    //IOPortedPrc 是輸出輸入跟 AZUSA 對接的隱藏進程
    class IOPortedPrc
    {
        //名字, 方便向用戶提示用的, 內部如果要識別各引擎應該用 pid
        public string Name;

        //pid 每個進程的惟一的 ID
        public int pid;

        //進程的實體
        Process Engine;

        //進程的類別
        public ProcessType Type;

        //進程可以向 AZUSA 登錄開放的接口位置 (TCP)
        //然後 AZUSA 記錄在 Ports, 有需要時向其他進程通報
        public List<string> Ports = new List<string>();

        //進程可以接管指令
        //RIDs 記錄的是進程接管的指令
        //第二個 bool 記錄的是進程是否只需要指令的參數
        //比如說進程只接管一個指令的話, 指令名其實是不必要的, AZUSA 只傳參數就好, 這樣的話可以把它設成 true
        public Dictionary<string, bool> RIDs = new Dictionary<string, bool>();


        public IOPortedPrc(string name, string enginePath, string arg = "")
        {
            //名字
            Name = name;

            //在進程沒有進一步宣告前先假定其為 Routine
            //default type is Routine until the Engine self-identifies itself
            Type = ProcessType.Routine;

            //這裡是創建進程實體的部分
            //specifies the way the recognizer is run
            Engine = new Process();
            Engine.StartInfo.FileName = enginePath;
            Engine.StartInfo.Arguments = arg;

            //把進程的工作路徑設成進程本身的路徑, 而不是預設的 AZUSA 的路徑
            Engine.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(enginePath);

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
        public StreamWriter Input{
            get {
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

        //結束進程
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
            if (Type == ProcessType.AI)
            {
                ProcessManager.AIPid.Remove(pid);
            }
            else if (Type == ProcessType.Input)
            {
                ProcessManager.InputPid.Remove(pid);
            }
            else if (Type == ProcessType.Output)
            {
                ProcessManager.OutputPid.Remove(pid);
            }

            //釋放變量佔用的資源
            Name = null;
            if (Ports != null)
            {
                Ports.Clear();
                Ports = null;
            }
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

            //移除事件監聽
            Engine.OutputDataReceived -= Engine_OutputDataReceived;
            Engine.Exited -= Engine_Exited;

            //拋棄進程的實體
            Engine.Dispose();
            Engine = null;

            //然後檢查引擎類型, 再從 ProcessManager 相應的名單中除名
            if (Type == ProcessType.AI)
            {
                ProcessManager.AIPid.Remove(pid);
            }
            else if (Type == ProcessType.Input)
            {
                ProcessManager.InputPid.Remove(pid);
            }
            else if (Type == ProcessType.Output)
            {
                ProcessManager.OutputPid.Remove(pid);
            }

            //釋放變量佔用的資源
            Name = null;
            if (Ports != null)
            {
                Ports.Clear();
                Ports = null;
            }
            if (RIDs != null)
            {
                RIDs.Clear();
                RIDs = null;
            }

            //從 ProcessManager 的進程名單中除名
            ProcessManager.RemoveProcess(this);
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

            //如果是詢問, 則調用 MUTAN 表達式解析器, 並返回結東
            //詢問的語法是 "(表達式)?"
            //First check if the engine is asking a question about value of an expression
            if (e.Data.EndsWith("?"))
            {
                string result;

                //去掉最後的問號, 就是表達式了
                //如果格式有誤的話, 會返回 INVALIDEXPR (無效的表達式) 或 IMBALBRACKET (括號不平衡)
                MUTAN.ExprParser.TryParse(e.Data.TrimEnd('?'), out result);
                Engine.StandardInput.WriteLine(result);
                return;
            }

            //否則先假設是 MUTAN 指令,嘗試解析, 如果失敗的話, 就無視掉本次輸出
            //If no then assume it is a MUTAN command and try parsing, if failed to parse, ignore.

            //先創建一個可運行物件, 用來儲存解析結果
            MUTAN.IRunnable obj;

            //然後用單行解析器
            if (MUTAN.LineParser.TryParse(e.Data, out obj))
            {
                //如果成功解析, 則運行物件, 獲取回傳碼
                MUTAN.ReturnCode[] returns = obj.Run();

                //然後按回傳碼執行指令
                foreach (MUTAN.ReturnCode code in returns)
                {
                    //首先是 NYAN 指令組的指令, NYAN 指令組主要是用來讓進程宣佈自己的角色和功能, 並取得可用接口等等的溝通協調用的指令
                    //Handle NYAN protocol related commands, leave the rest to AZUSA internals
                    switch (code.Command)
                    {
                        //這是除錯用的, 請無視
                        case "Debugging":
                            ProcessManager.AIPid.Add(pid);
                            ProcessManager.InputPid.Add(pid);
                            ProcessManager.OutputPid.Add(pid);
                            break;
                        //這是用來讓進程取得 AZUSA 的 pid, 進程可以利用 pid 檢查 AZUSA 是否存活, 當 AZUSA 意外退出時, 進程可以檢查到並一併退出
                        case "GetAzusaPid":
                            Engine.StandardInput.WriteLine(Process.GetCurrentProcess().Id);
                            break;
                        //這是讓進程宣佈自己的身份的, 這指令應該是進程完成各種初始化之後才用的
                        case "RegisterAs":
                            switch (code.Argument)
                            {
                                case "AI":
                                    this.Type = ProcessType.AI;
                                    ProcessManager.AIPid.Add(pid);
                                    break;
                                case "Input":
                                    this.Type = ProcessType.Input;
                                    ProcessManager.InputPid.Add(pid);
                                    break;
                                case "Output":
                                    this.Type = ProcessType.Output;
                                    ProcessManager.OutputPid.Add(pid);
                                    break;
                                case "Routine":
                                    this.Type = ProcessType.Routine;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        //這是讓進程宣佈自己的可連接的接口, AZUSA 記錄後可以轉告其他進程, 進程之間可以直接對接而不必經 AZUSA
                        case "RegisterPort":
                            this.Ports.Add(code.Argument);
                            break;
                        //這是讓進程取得當前可用的AI 端口(AI引擎的接口)
                        case "GetAIPorts":
                            string result = "";
                            foreach (IOPortedPrc prc in ProcessManager.GetCurrentProcesses())
                            {
                                if (prc.Type == ProcessType.AI)
                                {
                                    foreach (string port in prc.Ports)
                                    {
                                        result += port + ",";
                                    }
                                }
                            }

                            Engine.StandardInput.WriteLine(result.Trim(','));
                            break;
                        //這是讓進程取得當前可用的輸入端口(輸入引擎的接口)
                        case "GetInputPorts":
                            result = "";
                            foreach (IOPortedPrc prc in ProcessManager.GetCurrentProcesses())
                            {
                                if (prc.Type == ProcessType.Input)
                                {
                                    foreach (string port in prc.Ports)
                                    {
                                        result += port + ",";
                                    }
                                }
                            }

                            Engine.StandardInput.WriteLine(result.Trim(','));
                            break;
                        //這是讓進程取得當前可用的輸出端口(輸出引擎的接口)
                        case "GetOutputPorts":
                            result = "";
                            foreach (IOPortedPrc prc in ProcessManager.GetCurrentProcesses())
                            {
                                if (prc.Type == ProcessType.Output)
                                {
                                    foreach (string port in prc.Ports)
                                    {
                                        result += port + ",";
                                    }
                                }
                            }

                            Engine.StandardInput.WriteLine(result.Trim(','));
                            break;
                        //這是讓進程可以宣佈自己責負甚麼函式, AZUSA 在接收到這種函件就會轉發給進程
                        //函式接管不是唯一的, 可以同時有多個進程接管同一個函式, AZUSA 會每個宣告了接管的進程都轉發一遍
                        case "LinkRID":
                            string[] parsed = code.Argument.Split(',');

                            this.RIDs.Add(parsed[0], Convert.ToBoolean(parsed[1]));

                            break;
                        //如果不是上面的 NYAN 指令組的指令的話, 就要判斷 AZUSA 是否完整 (AI, 輸入, 輸出)
                        //如果完整就把指令傳達到內部執行
                        //否則的話為避免出錯, AZUSA 會無視掉
                        default:
                            if (ProcessManager.CheckCompleteness())
                            {
                                Internals.Execute(code.Command, code.Argument);
                            }

                            break;
                    }
                }
            }

        }



    }
}
