using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AZUSA
{
    //事件記錄
    class ActivityLog
    {
        static Queue<string> log = new Queue<string>();
        static FileStream logfile = new FileStream("log.txt", FileMode.OpenOrCreate);
        static StreamWriter logsw = new StreamWriter(logfile,Encoding.UTF8);

        static private object logMUTEX = new object();

        static public bool HasMore()
        {
            return log.Count != 0;
        }

        static public string Next()
        {
            lock (logMUTEX)
            {
                return log.Dequeue();
            }
        }

        static public void Add(string entry)
        {
            lock (logMUTEX)
            {
                log.Enqueue(entry);
                logsw.WriteLine(entry);
            }

        }
    }
}
