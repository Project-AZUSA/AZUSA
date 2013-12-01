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
    public partial class ActivityViewer : Form
    {
        public ActivityViewer()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Internals.EXITFLAG) { this.Close(); }
            while (ActivityLog.HasMore())
            {
                listBox1.Items.Insert(0, ActivityLog.Next());
            }
        }

        private void LogViewer_Load(object sender, EventArgs e)
        {
            this.Text = Localization.GetMessage("ACTMON", "Activity Monitor");
        }
    }
}
