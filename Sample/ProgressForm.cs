using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Sample
{
    public partial class ProgressForm : Form
    {

        public ProgressForm(string titleText)
        {
            InitializeComponent();
            mainLabel.Text = titleText;
        }
         
        public void StartScroll()
        {
            progressBar.Style = ProgressBarStyle.Marquee;
        }

        public void StopScroll()
        {
            progressBar.Style = ProgressBarStyle.Continuous;
        }

        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        private void ProgressForm_Load(object sender, EventArgs e)
        {
            this.Owner.Enabled = false;
        }

        private void ProgressForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Owner.Enabled = true;
        }
    }
}
