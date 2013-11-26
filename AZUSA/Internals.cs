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
    class Internals
    {

        static NotifyIcon notifyIcon = new NotifyIcon();

        static public void INIT()
        {
            //從 DATA 載入所有已儲存的變量
            //Load all the variables            
            Variables.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DATA");


            //創建提示圖標
            //Set up notify icon
            notifyIcon.Icon = AZUSA.Properties.Resources.icon;
            notifyIcon.Visible = true;

            //創建圖標右擊菜單的項目
            MenuItem itmRELD = new MenuItem("Reload"); //重新載入
            itmRELD.Click += new EventHandler(itmRELD_Click);
            MenuItem itmEXIT = new MenuItem("Exit"); //退出
            itmEXIT.Click += new EventHandler(itmEXIT_Click);
            ContextMenu menu = new ContextMenu(new MenuItem[] { itmEXIT, itmRELD });

            //把圖標右擊菜單設成上面創建的菜單
            notifyIcon.ContextMenu = menu;

            //搜索 Engines\ 底下的所有執行檔, SearchOption.AllDirectories 表示子目錄也在搜索範圍內
            //Start the engines
            string EngPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Engines";
            string[] EngList = System.IO.Directory.GetFiles(EngPath, "*.exe", SearchOption.AllDirectories);

            //每一個執行檔都添加為引擎
            foreach (string exePath in EngList)
            {
                ProcessManager.AddProcess(exePath.Replace(EngPath + @"\", "").Replace(".exe", "").Trim(), exePath);
            }

            //等待一秒鐘, 讓各引擎做好初始化和登錄
            System.Threading.Thread.Sleep(1000);

            //一秒後如果 AI, 輸入, 輸出不齊備的話就對用戶作出提示
            //如果引擎不齊備的話, 所有 NYAN 指令組以外的指令不會被執行
            //NYAN 指令組的具體內容請看 IOPortedPrc
            if (!ProcessManager.CheckCompleteness())
            {
                notifyIcon.ShowBalloonTip(1000, "AZUSA", "Some engines are missing. AZUSA will not function unless AI and I/O are all registered.", ToolTipIcon.Error);
            }

            //初始化到此結束, 然後就是各 IOPortedPrc 聽取和執行引擎的指令了
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
            notifyIcon.ShowBalloonTip(5000, "AZUSA", msg, ToolTipIcon.Error);
        }

        //發出普通提示
        static public void MESSAGE(string msg)
        {
            notifyIcon.ShowBalloonTip(5000, "AZUSA", msg, ToolTipIcon.Info);
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
                // LOOP({line}) 創建單行循環線程
                case "LOOP":
                    string[] content = new string[] { arg };
                    ThreadManager.AddLoop(content);
                    break;
                // MLOOP({block}) 創建多行循環線程
                case "MLOOP":
                    ThreadManager.AddLoop(arg.Split('\n'));
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
                        Internals.ERROR("Unable to find the script named " + scr[0] + ". Please make sure it is in the correct folder.");
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
                    scr = null;

                    //解析結果不為空的話就執行
                    //否則就報錯
                    if (obj != null)
                    {
                        foreach (MUTAN.ReturnCode code in obj.Run())
                        {
                            Execute(code.Command, code.Argument);
                        }

                        //扔掉物件
                        obj = null;
                    }
                    else
                    {
                        ERROR("An error occured while running script named " + scr[0] + ". Please make sure there is no syntax error.");
                    }
                    break;
                // WAIT({int}) 暫停線程
                case "WAIT":
                    System.Threading.Thread.Sleep(Convert.ToInt32(arg));
                    break;
                // ERR({expr}) 發送錯誤信息
                case "ERR":
                    ERROR(arg);
                    break;
                // MSG({expr}) 發送信息
                case "MSG":
                    MESSAGE(arg);
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
                default:
                    //如果不是系統指令, 先檢查是否有引擎登記接管了這個指令
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
                                    prc.Input.WriteLine(arg);
                                }
                                else
                                {
                                    prc.Input.WriteLine(cmd + "(" + arg + ")");
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
                        //否則的話就當成函式呼叫
                        ProcessManager.AddProcess(cmd, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Routines\" + cmd, arg);
                    }
                    break;
            }

        }

    }
}
