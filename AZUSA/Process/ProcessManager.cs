using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AZUSA
{
    //進程管理員
    static class ProcessManager
    {
        //現在運行中的進程
        static List<IOPortedPrc> CurrentProcesses = new List<IOPortedPrc>();

        //AI, 輸入, 輸出 引擎的 Pid
        static public List<int> AIPid = new List<int>();
        static public List<int> InputPid = new List<int>();
        static public List<int> OutputPid = new List<int>();

        //檢查引擎是否完備, 如果 AI, 輸入, 輸出 三者俱備才會返回 true
        static public bool CheckCompleteness()
        {
            return AIPid.Count != 0 && InputPid.Count != 0 && OutputPid.Count != 0;
        }

        //創建新進程, name 名字, enginePath 執行檔的路徑, arg 執行參數
        static public void AddProcess(string name, string enginePath, string arg = "")
        {
            //利用參數, 創建一個新的 IOPortedPrc
            IOPortedPrc prc = new IOPortedPrc(name, enginePath, arg);

            //嘗試啟動進程
            //如果成功, 把進程添加進 CurrentProcesses
            //如果失敗, 發出錯誤提示
            try
            {
                prc.Start();
                CurrentProcesses.Add(prc);
            }
            catch
            {
                Internals.ERROR("Unable to run " + name + ". Please make sure it is in the correct folder.");
                return;
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

            Refresh();
            
            return;
        }


        //通知所有進程退出
        static public void KillAll()
        {
            string name = System.Threading.Thread.CurrentThread.Name;
            List<IOPortedPrc> ListCopy = new List<IOPortedPrc>(CurrentProcesses);

            foreach (IOPortedPrc prc in ListCopy)
            {
                prc.End();
            }

            //扔掉 ListCopy
            ListCopy = null;

            Refresh();
        }


        //返回現在執行中的所有進程
        static public List<IOPortedPrc> GetCurrentProcesses()
        {
            return CurrentProcesses;
        }

        //刷新登錄, 清除掉已退出的進程
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
