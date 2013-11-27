using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZUSA
{
    class ActivityLog
    {
        static Queue<string> log = new Queue<string>();

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
            }

        }
    }
}
