using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Reflection;

namespace AZUSA
{
    public class AZUSAAppContext : ApplicationContext
    {
        

        public AZUSAAppContext()
        {
            //初始化
            Internals.INIT();      
        }
    }
}
