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

        //用來向其他表單宣佈主進程已退出
        static public bool EXITFLAG = false;

        //記錄圖標是否被點擊, 利用這個變量可以透過圖標跟用戶進行簡單的交互
        static public bool Clicked = false;

        //初始化
        static public void INIT()
        {
            //從 DATA 載入所有已儲存的變量
            //Load all the variables            
            Variables.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DATA");

            Variables.Write("$SYS_READY", "FALSE");

            //載入提示信息
            Localization.Initialize();

            //創建提示圖標
            //Set up notify icon
            notifyIcon.Icon = AZUSA.Properties.Resources.icon;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += new EventHandler(notifyIcon_DoubleClick);

            //創建圖標右擊菜單的項目
            MenuItem itmMonitor = new MenuItem(Localization.GetMessage("PRCMON", "Process Monitor")); //進程監視器
            itmMonitor.Click += new EventHandler(itmMonitor_Click);
            MenuItem itmActivity = new MenuItem(Localization.GetMessage("ACTMON", "Activity Monitor")); //活動監視器
            itmActivity.Click += new EventHandler(itmActivity_Click);
            MenuItem itmRELD = new MenuItem(Localization.GetMessage("RELOAD", "Reload")); //重新載入
            itmRELD.Click += new EventHandler(itmRELD_Click);
            MenuItem itmEXIT = new MenuItem(Localization.GetMessage("EXIT", "Exit")); //退出
            itmEXIT.Click += new EventHandler(itmEXIT_Click);
            MenuItem sep = new MenuItem("-");

            ContextMenu menu = new ContextMenu(new MenuItem[] { itmMonitor, itmActivity, sep, itmRELD, itmEXIT });

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

            //提示本體啟動成功, 待各引擎啟動完畢後會再有提示的
            //MESSAGE(Localization.GetMessage("AZUSAREADY", "AZUSA is ready. Waiting for engines to initialize..."));


            //每一個執行檔都添加為引擎
            foreach (string exePath in EngList)
            {
                //如果不成功就發錯誤信息
                if (!ProcessManager.AddProcess(exePath.Replace(EngPath + @"\", "").Replace(".exe", "").Trim(), exePath))
                {
                    Internals.ERROR(Localization.GetMessage("ENGSTARTFAIL", "Unable to run {arg}. Please make sure it is in the correct folder.", exePath.Replace(EngPath + @"\", "").Replace(".exe", "").Trim()));
                }

                Thread.Sleep(160);
            }

            
            

        }

        static void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Clicked = true;
        }

        static void itmActivity_Click(object sender, EventArgs e)
        {
            new ActivityViewer().Show();
        }

        static void itmMonitor_Click(object sender, EventArgs e)
        {
            new ProcessViewer().Show();
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

                //activity log
                ActivityLog.Add("AZUSA: " + msg);
            }
        }

        //發出普通提示
        static public void MESSAGE(string msg)
        {
            if (msg != "")
            {
                notifyIcon.ShowBalloonTip(5000, "AZUSA", msg, ToolTipIcon.Info);

                //activity log
                ActivityLog.Add("AZUSA: " + msg);
            }
        }

        //增加右鍵選單的選項
        static public void ADDMENUITEM(string name)
        {
            MenuItem itm = new MenuItem(Localization.GetMessage(name, name));
            itm.Name = name;
            itm.Click += new EventHandler(itm_Click);

            if (notifyIcon.ContextMenu.MenuItems.Count == 5)
            {
                notifyIcon.ContextMenu.MenuItems.Add(3, new MenuItem("-"));
            }

            notifyIcon.ContextMenu.MenuItems.Add(3, itm);

        }

        static void itm_Click(object sender, EventArgs e)
        {
            MenuItem itm = (MenuItem)sender;
            ProcessManager.Broadcast(itm.Text);
        }

        //這是用來暫存反應的
        static string respCache = "";

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
                // ACTVIEW() 打開活動檢視器
                case "ACTVIEW":
                    itmActivity_Click(null,EventArgs.Empty);
                    break;
                // PRCMON() 打開進程檢視器
                case "PRCMON":
                    itmMonitor_Click(null, EventArgs.Empty);
                    break;
                // EXEC(filepath,IsApp) 創建進程
                case "EXEC":
                    string patharg = arg.Replace("{AZUSA}",Environment.CurrentDirectory);
                    bool isapp;
                    if (arg.Contains(','))
                    {
                        patharg = arg.Split(',')[0];                        
                    }
                    if (!Boolean.TryParse(arg.Replace(patharg + ",", ""), out isapp))
                    {
                        isapp = true;
                    }
                    patharg = patharg.Trim();
                    string path = patharg;
                    string Arg = "";
                    if (patharg.Contains('$'))
                    {
                        path = patharg.Split('$')[0];
                        Arg = patharg.Replace(path + "$", "");
                    }

                    ProcessManager.AddProcess(Path.GetFileNameWithoutExtension(path), path, Arg, isapp);
                    break;
                // KILL(prcName) 終止進程
                case "KILL":
                    ProcessManager.Kill(arg);
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
                        program = File.ReadAllLines(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Scripts\" + scr[0] + ".mut");
                    }
                    catch
                    {
                        Internals.ERROR(Localization.GetMessage("SCRMISSING", "Unable to find the script named {arg}. Please make sure it is in the correct folder.", scr[0]));
                        return;
                    }

                    //如果有分塊的話, 就先進行提取
                    if (scr.Length > 1)
                    {
                        for (int i = 1; i < scr.Length; i++)
                        {
                            program = MUTAN.GetPart(program, scr[i]);
                        }
                    }

                    //然後進行解析
                    MUTAN.Parser.TryParse(program, out obj);

                    //清理暫存
                    program = null;

                    //解析結果不為空的話就執行
                    //否則就報錯
                    if (obj != null)
                    {
                        MUTAN.ReturnCode tmp = obj.Run();

                        if (tmp.Command != "END")
                        {
                            Execute(tmp.Command, tmp.Argument);
                        }


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
                    //把 $WAITFORRESP 設成 TRUE
                    Variables.Write("$WAITFORRESP", "TRUE");
                    //通知引擎 (主要是針對 AI) 現在正等待回應
                    ProcessManager.Broadcast("WaitingForResp");

                    respCache = arg;
                    break;
                // 作出反應
                case "MAKERESP":
                    //把 $WAITFORRESP 設成 FALSE
                    Variables.Write("$WAITFORRESP", "FALSE");
                    Variables.Write("$RESP", arg);

                    if (respCache == "")
                    {
                        break;
                    }

                    //通知引擎已作出反應
                    ProcessManager.Broadcast("RESPONDED");

                    //解析暫存
                    MUTAN.Parser.TryParse(respCache.Split('\n'), out obj);

                    //清空暫存
                    respCache = "";

                    //解析結果不為空的話就執行
                    //否則就報錯
                    if (obj != null)
                    {
                        MUTAN.ReturnCode tmp = obj.Run();

                        if (tmp.Command != "END")
                        {
                            Execute(tmp.Command, tmp.Argument);
                        }

                        //扔掉物件
                        obj = null;
                    }
                    else
                    {
                        ERROR(Localization.GetMessage("SCRERROR", "An error occured while running a response script. Please make sure there is no syntax error. [MUTAN, " + respCache + "]"));
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
                        //否則的話就當成函式呼叫, 如果有註明副檔名的就根據副檔名執行
                        if (cmd.Contains('.'))
                        {
                            switch (cmd.Split('.')[1])
                            {
                                case "bat":
                                    ProcessManager.AddProcess(cmd, "cmd.exe", "/C \"" + Environment.CurrentDirectory + @"\Routines\" + cmd + "\" " + arg);
                                    return;
                                case "vbs":
                                    ProcessManager.AddProcess(cmd, "cscript.exe", " \"" + Environment.CurrentDirectory + @"\Routines\" + cmd + "\" " + arg);
                                    return;
                            }
                        }

                        //如果沒副檔名就先找 exe
                        if (!ProcessManager.AddProcess(cmd, Environment.CurrentDirectory + @"\Routines\" + cmd + ".exe", arg))
                        {
                            //再找 bat (利用 bat 可以呼叫基本上任何直譯器調用任何腳本語言了)
                            if (File.Exists(Environment.CurrentDirectory + @"\Routines\" + cmd + ".bat"))
                            {
                                ActivityLog.Add("Calling \"" + "cmd.exe "+ "/C \"" + Environment.CurrentDirectory + @"\Routines\" + cmd + ".bat\" " + arg + "\"");
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
