using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CQFollowerAutoclaimer
{
    public partial class WBLog : Form
    {
        public WBLog(ref string s)
        {
            InitializeComponent();
            richTextBox1.Text = s;
        }

        private void WBLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            this.Parent = null;
            e.Cancel = true;
        }
        
    }
}
