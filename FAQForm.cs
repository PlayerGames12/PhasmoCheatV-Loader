using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhasmoCheatV_Loader
{
    public partial class FAQForm : Form
    {
        public static string FAQText = "Waiting loading...";
        public FAQForm()
        {
            InitializeComponent();
            loadText();
            guna2TextBox1.ReadOnly = true;
            guna2TextBox1.TabStop = false;
            guna2TextBox1.Enter += (s, e) => this.ActiveControl = null;
            guna2TextBox1.MouseDown += (s, e) => guna2TextBox1.SelectionLength = 0;
            guna2TextBox1.GotFocus += (s, e) => guna2TextBox1.SelectionLength = 0;
        }

        private void loadText()
        {
            guna2TextBox1.Text = FAQText;
        }

        private void DoneBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
