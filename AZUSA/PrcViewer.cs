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
    public partial class ProcessViewer : Form
    {
        public ProcessViewer()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Internals.EXITFLAG) { this.Close(); }
            listBox.Items.Clear();
            foreach(IOPortedPrc prc in ProcessManager.GetCurrentProcesses()){
                listBox.Items.Add("["+prc.currentType+"] "+prc.Name); 
            }
        }

        private void Monitor_Load(object sender, EventArgs e)
        {
            this.Text = Localization.GetMessage("PRCMON", "Process Monitor");
        }
    }
}
