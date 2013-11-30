using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;

namespace AZUSA
{
    // AZUSA 的內部功能
    class Internals
    {
        static NotifyIcon notifyIcon = new NotifyIcon();

        static public bool Debugging = false;

        static public bool EXITFLAG = false;

        //記錄圖標是否被點擊, 利用這個變量可以透過圖標跟用戶進行簡單的交互
        static public bool Clicked = false;

        //初始化
        static public void INIT()
        {
            //從 DATA 載入所有已儲存的變量
            //Load all the variables            
            Variables.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DATA");

            //載入提示信息
            Localization.Initialize();

            //創建提示圖標
            //Set up notify icon
            notifyIcon.Icon = AZUSA.Properties.Resources.icon;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += new EventHandler(notifyIcon_DoubleClick);

            //創建圖標右擊菜單的項目
            MenuItem itmMonitor = new MenuItem("Process Monitor"); //進程監視器
            itmMonitor.Click += new EventHandler(itmMonitor_Click);
            MenuItem itmActivity = new MenuItem("Activity Monitor"); //活動監視器
            itmActivity.Click += new EventHandler(itmActivity_Click);
            MenuItem itmRELD = new MenuItem("Reload"); //重新載入
            itmRELD.Click += new EventHandler(itmRELD_Click);
            MenuItem itmEXIT = new MenuItem("Exit"); //退出
            itmEXIT.Click += new EventHandler(itmEXIT_Click);

            ContextMenu menu = new ContextMenu(new MenuItem[] { itmMonitor, itmActivity, itmEXIT, itmRELD });

            //把圖標右擊菜單設成上面創建的菜單
            notifyIcon.ContextMenu = menu;

            //搜索 Engines\ 底下的所有執行檔, SearchOption.AllDirectories 表示子目錄也在搜索範圍內
            //Start the engines
            string EngPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Engines";
            string[] EngList = new string[] { };
            if (Directory.Exists(EngPath))
            {
                EngList = System.IO.Directory.GetFiles(EngPath, "*.exe", SearchOption.AllDirectories);
            }
            else
            {
                ERROR(Localization.GetMessage("ENGPATHMISSING", "The \\Engines folder is missing. AZUSA will not be able to perform any function without suitable engines."));

                return;
            }

            //每一個執行檔都添加為引擎
            foreach (string exePath in EngList)
            {
                //如果不成功就發錯誤信息
                if (!ProcessManager.AddProcess(exePath.Replace(EngPath + @"\", "").Replace(".exe", "").Trim(), exePath))
                {
                    Internals.ERROR(Localization.GetMessage("ENGSTARTFAIL", "Unable to run {arg}. Please make sure it is in the correct folder.", exePath.Replace(EngPath + @"\", "").Replace(".exe", "").Trim()));
                }
            }

            //提示本體啟動成功, 待各引擎啟動完畢後會再有提示的
            MESSAGE(Localization.GetMessage("AZUSAREADY", "AZUSA is ready. Waiting for engines to initialize..."));

        }

        static void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Clicked = true;
        }

        static void itmActivity_Click(object sender, EventArgs e)
        {
            new LogViewer().Show();
        }

        static void itmMonitor_Click(object sender, EventArgs e)
        {
            new Monitor().Show();
        }

        static void itmRELD_Click(object sender, EventArgs e)
        {
            RESTART();
        }
        static void itmEXIT_Click(object sender, EventArgs e)
        {
            EXIT();
        }

        //結束程序
        static public void EXIT()
        {
            EXITFLAG = true;

            //中止一切線程
            ThreadManager.BreakAll();

            //結束一切進程
            ProcessManager.KillAll();

            //線程和進程結束後, 變數的值就不可能再有變化了
            //此時可以保存變數的值
            Variables.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DATA");

            //拋棄圖標
            notifyIcon.Dispose();
            notifyIcon = null;

            //處理完畢, 可以通知程序退出
            Application.Exit();
        }

        //重啟程序
        static public void RESTART()
        {
            //中止一切線程
            ThreadManager.BreakAll();

            //等待所有線程退出完成
            while (ThreadManager.GetCurrentLoops().Count != 0)
            {
            }

            //結束一切進程
            ProcessManager.KillAll();

            //線程和進程結束後, 變數的值就不可能再有變化了
            //此時可以保存變數的值
            Variables.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DATA");

            //拋棄圖標
            notifyIcon.Dispose();
            notifyIcon = null;

            //處理完畢, 可以通知程序重啟
            Application.Restart();
        }

        //發出錯誤提示
        static public void ERROR(string msg)
        {
            if (msg != "")
            {
                notifyIcon.ShowBalloonTip(5000, "AZUSA", msg, ToolTipIcon.Error);
            }
        }

        //發出普通提示
        static public void MESSAGE(string msg)
        {
            if (msg != "")
            {
                notifyIcon.ShowBalloonTip(5000, "AZUSA", msg, ToolTipIcon.Info);
            }
        }


        //執行指令
        static public void Execute(string cmd, string arg)
        {
            //如果是空白指令就直接無視掉就行
            if (cmd.Trim() == "")
            {
                return;
            }

            //Internal commands
            switch (cmd)
            {
                // VAR({id}={expr}) 對變數進行寫入
                case "VAR":
                    string ID = arg.Split('=')[0];
                    string val = arg.Replace(ID + "=", "").Trim();
                    Variables.Write(ID, val);
                    break;                
                // MLOOP({block}) 創建多行循環線程
                case "LOOP":
                    ThreadManager.AddLoop(arg.Split('\n'));
                    break;
                // BROADCAST({expr}) 向所有引擎廣播消息
                case "BROADCAST":
                    ProcessManager.Broadcast(arg);
                    break;
                // EXIT() 退出程序
                case "EXIT":
                    //創建一個負責退出的線程
                    new Thread(new ThreadStart(EXIT)).Start();
                    break;
                // RESTART() 重啟程序
                case "RESTART":
                    //創建一個負責重啟的線程
                    new Thread(new ThreadStart(RESTART)).Start();
                    break;
                // WAIT({int}) 暫停線程
                case "WAIT":
                    System.Threading.Thread.Sleep(Convert.ToInt32(arg));
                    break;
                // SCRIPT({SID(.part)}) 執行腳本檔
                case "SCRIPT":
                    //創建執行物件
                    MUTAN.IRunnable obj;

                    //分割參數, 以便取得部分名, 例如 TEST.part1
                    string[] scr = arg.Split('.');

                    //用來暫存腳本內容的陣列
                    string[] program;

                    //首先嘗試讀入腳本內容
                    try
                    {
                        program = File.ReadAllLines(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Scripts\" + scr[0]);
                    }
                    catch
                    {
                        Internals.ERROR(Localization.GetMessage("SCRMISSING", "Unable to find the script named {arg}. Please make sure it is in the correct folder.", scr[0]));
                        return;
                    }

                    //然後如果 scr 有兩個元素的話, 表示帶有部分名, 只需解析要求的部分
                    if (scr.Length == 2)
                    {
                        MUTAN.Parser.TryParse(program, out obj, scr[1].Trim());
                    }
                    //否則就整個解析
                    else
                    {
                        MUTAN.Parser.TryParse(program, out obj);
                    }

                    //清理暫存
                    program = null;

                    //解析結果不為空的話就執行
                    //否則就報錯
                    if (obj != null)
                    {


                        foreach (MUTAN.ReturnCode code in obj.Run())
                        {
                            //腳本有特殊語法
                            switch (code.Command)
                            {
                                //中止執行
                                case "END":
                                    goto END;                                
                                //一般其他指令
                                default:
                                    Execute(code.Command, code.Argument);
                                    break;
                            }


                        }
                    END:
                        //扔掉物件
                        obj = null;
                    }
                    else
                    {
                        ERROR(Localization.GetMessage("SCRERROR", "An error occured while running script named {arg}. Please make sure there is no syntax error.", scr[0]));
                    }
                    break;
                //等待回應
                case "WAITFORRESP":
                    Variables.Write("$WAITFORRESP", "TRUE");
                    //通知引擎 (主要是針對 AI) 現在正等待回應
                    ProcessManager.Broadcast("WaitingForResp");

                    while (Convert.ToBoolean(Variables.Read("$WAITFORRESP"))) { }

                    MUTAN.Parser.TryParse(arg.Split('\n'), out obj);

                    //解析結果不為空的話就執行
                    //否則就報錯
                    if (obj != null)
                    {
                        foreach (MUTAN.ReturnCode code in obj.Run())
                        {
                            //腳本有特殊語法
                            switch (code.Command)
                            {
                                //中止執行
                                case "END":
                                    goto END;
                                //一般其他指令
                                default:
                                    Execute(code.Command, code.Argument);
                                    break;
                            }
                        }
                    END:
                        //扔掉物件
                        obj = null;
                    }
                    else
                    {
                        ERROR(Localization.GetMessage("SCRERROR", "An error occured while running script named {arg}. Please make sure there is no syntax error.", ""));
                    }
                    break;
                // ERR({expr}) 發送錯誤信息
                case "ERR":
                    //ERR 是屬於表現層的系統指令, 容許被接管
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
                                    prc.Input.WriteLine(arg);

                                    //activity log
                                    ActivityLog.Add("To " + prc.Name + ": " + arg);
                                }
                                else
                                {
                                    prc.Input.WriteLine(cmd + "(" + arg + ")");

                                    //activity log
                                    ActivityLog.Add("To " + prc.Name + ": " + cmd + "(" + arg + ")");
                                }
                            }
                        }
                        catch { }
                    }

                    //扔掉 ListCopy
                    ListCopy = null;

                    //否則就由圖標發出提示
                    if (!routed)
                    {
                        ERROR(arg);
                    }
                    break;
                // MSG({expr}) 發送信息
                case "MSG":
                    //MSG 是屬於表現層的系統指令, 容許被接管
                    routed = false;

                    ListCopy = new List<IOPortedPrc>(ProcessManager.GetCurrentProcesses());

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
                                    prc.Input.WriteLine(arg);

                                    //activity log
                                    ActivityLog.Add("To " + prc.Name + ": " + arg);
                                }
                                else
                                {
                                    prc.Input.WriteLine(cmd + "(" + arg + ")");

                                    //activity log
                                    ActivityLog.Add("To " + prc.Name + ": " + cmd + "(" + arg + ")");
                                }
                            }
                        }
                        catch { }
                    }

                    //扔掉 ListCopy
                    ListCopy = null;

                    //否則就由圖標發出提示
                    if (!routed)
                    {
                        MESSAGE(arg);
                    }
                    break;

                default:
                    //如果不是系統指令, 先檢查是否有引擎登記接管了這個指令
                    // routed 記錄指令是否已被接管
                    routed = false;

                    ListCopy = new List<IOPortedPrc>(ProcessManager.GetCurrentProcesses());

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
                                    prc.Input.WriteLine(arg);

                                    //activity log
                                    ActivityLog.Add("To " + prc.Name + ": " + arg);
                                }
                                else
                                {
                                    prc.Input.WriteLine(cmd + "(" + arg + ")");

                                    //activity log
                                    ActivityLog.Add("To " + prc.Name + ": " + cmd + "(" + arg + ")");
                                }
                            }
                        }
                        catch { }
                    }

                    //扔掉 ListCopy
                    ListCopy = null;

                    //所有進程都檢查完畢
                    //如果 routed 為 true, 那麼已經有進程接管了, AZUSA 就可以不用繼續執行
                    //No need to continue executing the command because it has been routed already
                    if (!routed)
                    {
                        //否則的話就當成函式呼叫, 先找 exe
                        if (!ProcessManager.AddProcess(cmd, Environment.CurrentDirectory + @"\Routines\" + cmd + ".exe", arg))
                        {
                            //再找 bat (利用 bat 可以呼叫基本上任何直譯器調用任何腳本語言了)
                            if (File.Exists(Environment.CurrentDirectory + @"\Routines\" + cmd + ".bat"))
                            {
                                ProcessManager.AddProcess(cmd, "cmd.exe", "/C \"" + Environment.CurrentDirectory + @"\Routines\" + cmd + ".bat\" " + arg);
                                //再找 vbs
                            }
                            else if (File.Exists(Environment.CurrentDirectory + @"\Routines\" + cmd + ".vbs"))
                            {
                                ProcessManager.AddProcess(cmd, "cscript.exe", " \"" + Environment.CurrentDirectory + @"\Routines\" + cmd + ".vbs\" " + arg);
                                //都找不到就報錯
                            }
                            else
                            {
                                Internals.ERROR(Localization.GetMessage("ENGSTARTFAIL", "Unable to run {arg}. Please make sure it is in the correct folder.", cmd));
                            }
                        }
                    }
                    break;
            }

        }

    }
}
