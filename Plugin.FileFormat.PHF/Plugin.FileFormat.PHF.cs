using System;
using System.Text;
using System.IO;
using Common;

namespace Plugin.FileFormat.PHF
{
    public class PHF : Plugins.FileFormat
    {
        byte[] rawBinary;
        
        public string FileDescription
        {
            get
            {
                return "IDS PHF";
            }
        }

        public string FileExtension
        {
            get
            {
                return "phf";
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
                var bytes = File.ReadAllBytes(fileName);

                int outputSize = -1;
                var header = new byte[0x12];
                var spanishOak = ASCIIEncoding.ASCII.GetBytes("SPANISHOAK");
                var blackOak = ASCIIEncoding.ASCII.GetBytes("BOAK");
                var greenOak = ASCIIEncoding.ASCII.GetBytes("GOAK");
                if (BinaryHelper.FindBytes(bytes, spanishOak, 0x100) != -1)
                {
                    header[0x10] = 0x10;
                    header[0x11] = 0x60;
                    outputSize = 1048576;
                }
                else if (BinaryHelper.FindBytes(bytes, blackOak, 0x100) != -1)
                {
                    header[0x10] = 0x30;
                    header[0x11] = 0x60;
                    outputSize = 1572864;
                }
                else if (BinaryHelper.FindBytes(bytes, greenOak, 0x100) != -1)
                {
                    header[0x10] = 0x30;
                    header[0x11] = 0x60;
                    outputSize = 1572864;
                }
                else return false;

                rawBinary = new byte[outputSize];

                //Find the magic header bytes
                int offset = BinaryHelper.FindBytes(bytes, header);
                if (offset == -1)
                {
                    return false;
                }

                int index = 0;
                for(int i = offset; i < bytes.Length;)
                {

                    //8 Byte header before we start again
                    if (index % 0x10000 == 0 && index != 0) i += 8;
                    if (index >= rawBinary.Length) break;

                    //32768-65536 is missing??
                    if (index >= 32768 && index < 65536)
                    {
                        rawBinary[index] = 0xFF;
                        index++;
                    } else
                    {
                        //Every 32 byte block we have a 6 byte header to remove
                        if (index % 32 == 0 && index != 0) i += 6;
                        if (index >= rawBinary.Length) break;

                        //TODO put detection for file length/type and do this for all types correctly not just spanish oak
                        rawBinary[index] = bytes[i];
                        i++;
                        index++;
                    }
                }

            } catch (Exception)
            {
                return false;
            }

            _fileOpen = true;

            //File.WriteAllBytes(@"C:\Dropbox\Car\Tunes\HACCEG3.phf.bin", rawBinary);

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
            throw new Exception("Save to PHF not yet implemented.");
            //File.WriteAllBytes(fileName, bytes);
        }
    }
}
