using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace J2534DotNet.Logger
{
    public partial class SelectDevice : Form
    {
        public SelectDevice()
        {
            InitializeComponent();
        }

        public J2534Device Device { get; set; }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Device = (J2534Device)deviceList.SelectedItem;
            Close();
        }

        private void SelectDevice_Load(object sender, EventArgs e)
        {
            deviceList.DataSource = J2534Detect.ListDevices();
            deviceList.DisplayMember = "Name";
        }
    }
}
