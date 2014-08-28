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

        private void ActivityViewer_Resize(object sender, EventArgs e)
        {
            if (splitContainer1.Size.Height > 26)
            {
                splitContainer1.SplitterDistance = splitContainer1.Size.Height - 26;
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

                if (textBox1.Text.EndsWith("?"))
                {
                    string val;
                    MUTAN.ExprParser.TryParse(textBox1.Text.TrimEnd('?'), out val);
                    textBox1.Text = val;
                    textBox1.SelectAll();
                    return;
                }

                ActivityLog.Add(textBox1.Text);

                MUTAN.IRunnable obj;
                MUTAN.LineParser.TryParse(textBox1.Text, out obj);

                textBox1.Text = "";

                if (obj == null)
                {
                    textBox1.Text = "ERR";
                    textBox1.SelectAll();
                    return;
                }

                MUTAN.ReturnCode tmp = obj.Run();

                if (tmp.Command != "")
                {
                    Internals.Execute(tmp.Command, tmp.Argument);
                }
            }

        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string head=listBox1.SelectedItem.ToString().Split(':')[0]+":";
            textBox1.Text=listBox1.SelectedItem.ToString().Replace(head,"").Trim();
        }

       

    }
}
