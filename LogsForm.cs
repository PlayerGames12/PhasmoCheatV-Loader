using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static PhasmoCheatV_Loader.MainForm;

namespace PhasmoCheatV_Loader
{
    public partial class LogsForm : Form
    {
        public LogsForm()
        {
            InitializeComponent();
            guna2TextBox1.Text = MainForm.logs;
            guna2TextBox1.ReadOnly = true;
            guna2TextBox1.TabStop = false;
            guna2TextBox1.Enter += (s, e) => this.ActiveControl = null;
            guna2TextBox1.MouseDown += (s, e) => guna2TextBox1.SelectionLength = 0;
            guna2TextBox1.GotFocus += (s, e) => guna2TextBox1.SelectionLength = 0;
        }

        private void DoneBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CopyBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string textToCopy = MainForm.logs;

                if (string.IsNullOrEmpty(textToCopy))
                {
                    MessageBox.Show("Nothing to copy.", "Error");
                    return;
                }

                Clipboard.SetText(textToCopy); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Copy failed: " + ex.Message, "Error");
            }
        }
    }
}