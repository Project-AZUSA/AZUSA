using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AZUSA
{
    public partial class Monitor : Form
    {
        public Monitor()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            listBox.Items.Clear();
            foreach(IOPortedPrc prc in ProcessManager.GetCurrentProcesses()){
                listBox.Items.Add(prc.Name); 
            }
        }
    }
}
