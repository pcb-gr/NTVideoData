using NTVideoData.Util;
using NTVideoData.Victims;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NTVideoData_v1
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
        }

        public void append(string logText)
        {
            if (UIUtil.ControlInvokeRequired(richTextBox, () => richTextBox.AppendText(logText + "\r\n"))) return;
            richTextBox.AppendText(logText + "\r\n");
        }

        private void LogForm_Load(object sender, EventArgs e)
        {

        }
    }
}
