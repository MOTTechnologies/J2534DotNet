using System;
using System.IO;

namespace Plugins
{
    public class BIN : Plugins.FileFormat
    {
        byte[] rawBinary;
        
        public string FileDescription
        {
            get
            {
                return "Binary File";
            }
        }

        public string FileExtension
        {
            get
            {
                return "bin";
            }
        }

        bool _fileOpen = false;
        public bool FileOpen
        {
            get
            {
                return _fileOpen;
            }
        }

        public bool Open(string fileName)
        {
            try
            {
                rawBinary = File.ReadAllBytes(fileName);

            } catch (Exception)
            {
                return false;
            }

            _fileOpen = true;

            return true;

        }

        public bool TryReadBytes(out byte[] bytes)
        {

            if (!_fileOpen || rawBinary == null)
            {
                bytes = new byte[0];
                return false;
            }

            bytes = rawBinary;
            return true;
        }

        public void WriteBytes(byte[] bytes, string fileName)
        {
            File.WriteAllBytes(fileName, bytes);
        }
    }
}
