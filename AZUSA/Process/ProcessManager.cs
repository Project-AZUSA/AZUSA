using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AZUSA
{
    //進程類型
    enum PortType { Input, Output, AI , Unknown}

    //進程管理員
    static class ProcessManager
    {
        //現在運行中的進程
        static List<IOPortedPrc> CurrentProcesses = new List<IOPortedPrc>();

        //所有已登錄的端口
        static public Dictionary<string, PortType> Ports=new Dictionary<string,PortType>();

        //AI, 輸入, 輸出 引擎的 Pid
        static public List<int> AIPid = new List<int>();
        static public List<int> InputPid = new List<int>();
        static public List<int> OutputPid = new List<int>();

        //檢查引擎是否完備, 如果 AI, 輸入, 輸出 三者俱備才會返回 true
        static public bool CheckCompleteness()
        {
            if (Internals.Debugging) { return true; }
            return AIPid.Count != 0 && InputPid.Count != 0 && OutputPid.Count != 0;
        }

        //創建新進程, name 名字, enginePath 執行檔的路徑, arg 執行參數, 返回是否成功
        static public bool AddProcess(string name, string enginePath, string arg = "", bool IsApplication=false)
        {
            //利用參數, 創建一個新的 IOPortedPrc
            IOPortedPrc prc = new IOPortedPrc(name, enginePath, arg);
            prc.IsApplication = IsApplication;

            //嘗試啟動進程
            //如果成功, 把進程添加進 CurrentProcesses, 返回 true
            //如果失敗, 返回 false   
            try
            {
                prc.Start();
                CurrentProcesses.Add(prc);
                return true;
            }
            catch
            {
                return false;
            }
        }

        //取消進程的登錄
        //此函數只從 CurrentProcesses 中移除指定的進程, 不實際結束進程
        //在 IOPortedPrc 的 End() 方法時會被調用到
        //這樣是因為在 IOPortedPrc 對進程退出做更好的處理
        //當 IOPortedPrc 判斷已成功退出, 再叫 ProcessManager 取消登錄
        static public void RemoveProcess(IOPortedPrc prc)
        {
            CurrentProcesses.Remove(prc);
            
            return;
        }


        //通知所有進程退出
        static public void KillAll()
        {
            List<IOPortedPrc> ListCopy = new List<IOPortedPrc>(CurrentProcesses);

            foreach (IOPortedPrc prc in ListCopy)
            {
                if (!prc.IsApplication)
                {
                    prc.End();
                }
            }

            //扔掉 ListCopy
            ListCopy = null;

        }

        //通知所有進程退出
        static public void Kill(string NAME)
        {
            List<IOPortedPrc> ListCopy = new List<IOPortedPrc>(CurrentProcesses);

            foreach (IOPortedPrc prc in ListCopy)
            {
                if (prc.Name==NAME)
                {
                    prc.End();
                }
            }

            //扔掉 ListCopy
            ListCopy = null;

        }

        //對進程進行廣播
        static public void Broadcast(string msg)
        {            
            List<IOPortedPrc> ListCopy = new List<IOPortedPrc>(ProcessManager.GetCurrentProcesses());

            foreach (IOPortedPrc prc in ListCopy)
            {
                prc.Input.WriteLine(msg);

                //activity log
                ActivityLog.Add("To " + prc.Name + ": "+msg);
            }

            ListCopy = null;
        }


        //返回現在執行中的所有進程
        static public List<IOPortedPrc> GetCurrentProcesses()
        {
            return CurrentProcesses;
        }

        //刷新登錄, 清除掉已退出的進程
        //**非必要請勿使用, 懷疑使用不當會導致內存泄漏, 具體原因不明
        static public void Refresh()
        {
            List<IOPortedPrc> ListCopy = new List<IOPortedPrc>(CurrentProcesses);

            foreach (IOPortedPrc prc in ListCopy)
            {
                if (prc.HasExited())
                {
                    CurrentProcesses.Remove(prc);
                }
            }

            //扔掉 ListCopy
            ListCopy = null;
        }


    }
}
