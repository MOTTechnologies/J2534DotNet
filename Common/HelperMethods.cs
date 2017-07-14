using System;
using System.IO;
using System.Windows.Forms;
using JR.Utils.GUI.Forms;
using System.Drawing;

namespace Common
{
    public static class HelperMethods
    {

        public static DateTime EpochToDateTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static Color HSVtoRGB(double h, double s, double v)
        {
            double r, g, b, f, p, q, t;
            int i = 0;
            if (s == 0)
            {
                return Color.FromArgb((byte)(v * 255.0), (byte)(v * 255.0), (byte)(v * 255.0));
            }
            h /= 60;            // sector 0 to 5
            i = (int)Math.Floor(h);
            f = h - i;          // factorial part of h
            p = v * (1.0 - s);
            q = v * (1.0 - s * f);
            t = v * (1.0 - s * (1.0 - f));
            switch ((int)i)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;
                default:        // case 5:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }
            return Color.FromArgb((byte)(r * 255.0), (byte)(g * 255.0), (byte)(b * 255.0));
        }



        private static double Mean(double[][] values, int index, int start, int end)
        {
            double s = 0;

            for (int i = start; i < end; i++) s += values[i][index];

            return s / (end - start);
        }


        private static double RootMeanSquare(double[][] values, int index)
        {
            double s = 0;
            int i;
            for (i = 0; i < values.Length; i++)
            {
                s += values[i][index] * values[i][index];
            }
            return Math.Sqrt(s / values.Length);
        }

        public static bool IsValidDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return false;
            return true;
        }

        public static bool WriteCSV(string filename, string [] csvHeaders, double[][] csvData)
        {
            // Get the file's text.
            try
            {
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    string header = "";
                    for (int column = 0; column < csvHeaders.Length; column++)
                    {
                        header += csvHeaders[column].ToString();
                        if (column != csvHeaders.Length - 1) header += ",";
                    }
                    writer.WriteLine(header);

                    for (int row = 0; row < csvData.Length; row++)
                    {
                        string rowString = "";
                        for (int column = 0; column < csvData[0].Length; column++)
                        {
                            if(IsValidDouble(csvData[row][column])) rowString += csvData[row][column].ToString();
                            if (column != csvHeaders.Length - 1) rowString += ",";
                        }
                        writer.WriteLine(rowString);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message, "Error");
                return false;
            }
        }

        // Load a CSV file into an array of rows and columns.
        // Assume there may be blank lines but every line has
        public static bool LoadCsv(string filename, out string[] _csvHeaders, out double[][] _csvData)
        {
            // Get the file's text.
            string [] csvHeaders = new string[0];
            double [][] csvData = new double[0][];

            try
            {
                string whole_file = System.IO.File.ReadAllText(filename);

                // Split into lines.
                whole_file = whole_file.Replace('\n', '\r');
                string[] lines = whole_file.Split(new char[] { '\r' },
                    StringSplitOptions.RemoveEmptyEntries);

                //Check if empty
                if (lines.Length < 2)
                {
                    MessageBox.Show("CSV file requires at least one entry", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (string.IsNullOrEmpty(lines[0]))
                {
                    MessageBox.Show("CSV file missing header", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                int headerRow = 0;
                int firstDataRow = 1;
                if (lines[0].Contains("HP Tuners CSV Log File"))
                {
                    //We have a HPL file
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Equals("[Channel Data]"))
                        {
                            firstDataRow = i + 1;
                        }
                        if (lines[i].Equals("[Channel Information]"))
                        {
                            headerRow = i + 2;
                        }
                        if (firstDataRow != 1 && headerRow != 0) break;

                        if (i == lines.Length - 2)
                        {
                            MessageBox.Show("Could not find [Channel Data] Or [Channel Information] within HPT CSV log file, giving up.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            return false;
                        }
                    }

                }

                // See how many rows and columns there are.
                int num_rows = lines.Length - firstDataRow - 1;
                int num_cols = lines[headerRow].Split(',').Length;

                // Allocate the data array.
                csvHeaders = new string[num_cols];
                csvData = new double[num_rows-1][];

                // Load the array.

                string[] headerLine = lines[headerRow].Split(',');
                if (headerLine.Length < 3)
                {
                    MessageBox.Show("Not enough headers in CSV file! Need at least three, possibly wrong file format? I'm giving up, sorry bro.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                csvHeaders = headerLine;

                //Parallel.For(firstDataRow, num_rows, r =>
                //{
                //    string[] line_r = lines[r].Split(',');
                //    csvData[r - 1] = new double[num_cols];
                //    for (int c = 0; c < num_cols; c++)
                //    {
                //        double result;
                //        if (String.IsNullOrEmpty(line_r[c])) continue;
                //        if (Double.TryParse(line_r[c], out result))
                //        {
                //            csvData[r - 1][c] = result;
                //        }
                //        else
                //        {
                //            csvData[r - 1][c] = double.NaN;
                //        }
                //    }
                //});
                for(int r = 0; r < num_rows; r++)
                {
                    string[] line_r = lines[r].Split(',');
                    csvData[r - 1] = new double[num_cols];
                    for (int c = 0; c < num_cols; c++)
                    {
                        double result;
                        if (String.IsNullOrEmpty(line_r[c])) continue;
                        if (Double.TryParse(line_r[c], out result))
                        {
                            csvData[r - 1][c] = result;
                        }
                        else
                        {
                            csvData[r - 1][c] = double.NaN;
                        }
                    }
                }


                return true;


            }
            catch (Exception e)
            {
                var currentStack = new System.Diagnostics.StackTrace(true);
                string stackTrace = currentStack.ToString();

                FlexibleMessageBox.Show("Failed to open file due to error: " + e.Message + Environment.NewLine + "Stacktrace: " + Environment.NewLine + stackTrace,
                                     "Error",
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Information,
                                     MessageBoxDefaultButton.Button2);
                return false;
            } finally
            {
                _csvHeaders = csvHeaders;
                _csvData = csvData;
            }
        }
    }
}
