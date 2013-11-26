using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AZUSA
{
    //循環線程
    class LoopThread
    {
       
        //線程的實體
        Thread thread;

        //被循環的執行物件
        MUTAN.IRunnable obj;

        //標示線程是否要中斷
        bool BREAKING = false;

        public LoopThread(string[] content)
        {            
            //把代碼解析成執行物件
            MUTAN.Parser.TryParse(content, out obj);

            //創建進程
            thread = new Thread(new ThreadStart(this.RunScript));

            //開始進程
            thread.Start();
        }

        //返回線程是否存活
        public bool IsAlive()
        {
            return thread.IsAlive;
        }

        //中斷線程
        public void Break()
        {
            //設 BREAKING 為 true
            BREAKING = true;

            //嘗試停止線程
            thread.Abort();

            //拋棄線程實體
            thread = null;

            //拋棄執行物件
            obj = null;

            //從管理員的名單中除名
            ThreadManager.RemoveLoop(this);
        }

        ~LoopThread()
        {
            obj = null;

            BREAKING = true;
            try
            {
                thread.Abort();
                thread = null;
            }
            catch { }

            ThreadManager.RemoveLoop(this);
        }

        //執行循環
        void RunScript()
        {
            //如果不是要中斷的話就一直重覆
            while (!BREAKING)
            {
                //執行物件, 取得返回碼
                foreach (MUTAN.ReturnCode code in obj.Run())
                {
                    //如果是 BREAK 指令, 就設 BREAK 為真, 退出循環
                    if (code.Command.Trim() == "BREAK")
                    {
                        BREAKING = true;
                        break;
                    }
                    //否則就執行指令
                    else
                    {
                        Internals.Execute(code.Command, code.Argument);
                    }
                }
            }

            //如果循環結束了就中斷線程
            Break();
        }
    }
}
