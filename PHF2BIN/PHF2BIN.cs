using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Plugins;
using System.IO;

namespace PHF2BIN
{
    public partial class PHF2BIN : Form
    {
        FileFormatPluginLoader pluginLoader;



        public PHF2BIN()
        {
            InitializeComponent();
            pluginLoader = new FileFormatPluginLoader(Directory.GetCurrentDirectory());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFile();

        }

        void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = pluginLoader.GetAllFormatsFilter();
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) return;
            var fileName = openFileDialog.FileName;

            string extension = Path.GetExtension(fileName).ToLowerInvariant().Replace(".", "");
            if (!File.Exists(fileName))
            {
                MessageBox.Show("File does not exist: " + fileName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] rawBinaryFile;

            Plugins.FileFormat fileFormatPlugin;

            if (pluginLoader.TryGetFileFormat(extension, out fileFormatPlugin))
            {
                if (fileFormatPlugin.Open(fileName))
                {
                    if (fileFormatPlugin.TryReadBytes(out rawBinaryFile))
                    {
                        if (rawBinaryFile.Length > 0)
                        {
                            SaveFile(rawBinaryFile);
                            return;
                        }
                    }
                }
            }
            MessageBox.Show("Invalid file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        void SaveFile(byte [] rawBinary)
        {
            SaveFileDialog savefile = new SaveFileDialog();

            //savefile.Filter = pluginLoader.GetIndividualFormatsFilter();
            savefile.Filter = "Binary File|*.bin";

            if (savefile.ShowDialog() == DialogResult.OK)
            {

                string extension = Path.GetExtension(savefile.FileName).ToLowerInvariant().Replace(".", "");

                Plugins.FileFormat fileFormatPlugin;
                if (pluginLoader.TryGetFileFormat(extension, out fileFormatPlugin))
                {
                    try
                    {
                        fileFormatPlugin.WriteBytes(rawBinary, savefile.FileName);

                        MessageBox.Show("Successfully Saved File!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Saving File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("File format not implemented", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


            }
        }




    }
}
