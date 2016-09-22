#region Copyright (c) 2016, Roland Harrison
/* 
 * Copyright (c) 2016, Roland Harrison
 * roland.c.harrison@gmail.com
 * 
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * Neither the name of the organization nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */
#endregion
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using J2534DotNet;
using OBD;

namespace Sample
{
    using J2534DotNet.Logger;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

    public partial class SimpleTest : Form
    {
        bool connected = false;
        J2534Extended passThru;
        UDSFord comm;
        public SimpleTest()
        {
            InitializeComponent();
            passThru = new J2534Extended();
        }


        private void CmdDetectDevicesClick(object sender, EventArgs e)
        {
            // Calling J2534.GetAvailableDevices() will return a list of installed J2534 devices
            List<J2534Device> availableJ2534Devices = J2534Detect.ListDevices();
            if (availableJ2534Devices.Count == 0)
            {
                MessageBox.Show("Could not find any installed J2534 devices.");
                return;
            }


            foreach (J2534Device device in availableJ2534Devices)
            {
                log.Text += device.Name + ", " + device.Vendor + "\r\n\r\n";
                log.Text += "\tConfig Application:\t" + device.ConfigApplication + "\r\n";
                log.Text += "\tFunction Library:\t" + device.FunctionLibrary + "\r\n\r\n";
                log.Text += "\tProtocol\t\tChannels\r\n";
                log.Text += "\tCAN\t\t" + device.CANChannels + "\r\n";
                log.Text += "\tISO15765\t" + device.ISO15765Channels + "\r\n";
                log.Text += "\tISO14230\t" + device.ISO14230Channels + "\r\n";
                log.Text += "\tISO9141\t\t" + device.ISO9141Channels + "\r\n";
                log.Text += "\tJ1850PWM\t" + device.J1850PWMChannels + "\r\n";
                log.Text += "\tJ1850PWM\t" + device.J1850VPWChannels + "\r\n";
                log.Text += "\tSCI_A_ENGINE\t" + device.SCI_A_ENGINEChannels + "\r\n";
                log.Text += "\tSCI_A_TRANS\t" + device.SCI_A_TRANSChannels + "\r\n";
                log.Text += "\tSCI_B_ENGINE\t" + device.SCI_B_ENGINEChannels + "\r\n";
                log.Text += "\tSCI_B_TRANS\t" + device.SCI_B_TRANSChannels + "\r\n\r\n";
            }
        }


        private void SendReceiveNoErrorChecking(object sender, EventArgs e)
        {
            J2534Extended passThru = new J2534Extended();

            if (!connected) Connect();

            // Attempt to open a communication link with the pass thru device
            int deviceId = 0;
            passThru.PassThruOpen(IntPtr.Zero, ref deviceId);

            // Open a new channel configured for ISO15765 (CAN)
            int channelId = 0;
            passThru.PassThruConnect(deviceId, ProtocolID.ISO15765, ConnectFlag.NONE, BaudRate.ISO15765_125000, ref channelId);

            // Set up a message filter to watch for response messages
            int filterId = 0;
            PassThruMsg maskMsg = new PassThruMsg(
                ProtocolID.ISO15765,
                TxFlag.ISO15765_FRAME_PAD,
                new byte[] { 0xff, 0xff, 0xff, 0xff });
            PassThruMsg patternMsg = new PassThruMsg(
                ProtocolID.ISO15765,
                TxFlag.ISO15765_FRAME_PAD,
                new byte[] { 0x00, 0x00, 0x07, 0xE8 });
            PassThruMsg flowControlMsg = new PassThruMsg(
                ProtocolID.ISO15765,
                TxFlag.ISO15765_FRAME_PAD,
                new byte[] { 0x00, 0x00, 0x07, 0xE0 });

            IntPtr maskMsgPtr = maskMsg.ToIntPtr();
            IntPtr patternMsgPtr = patternMsg.ToIntPtr();
            IntPtr flowControlMsgPtr = flowControlMsg.ToIntPtr();
            passThru.PassThruStartMsgFilter(channelId, FilterType.FLOW_CONTROL_FILTER, maskMsgPtr, patternMsgPtr, flowControlMsgPtr, ref filterId);

            // Clear out the response buffer so we know we're getting the freshest possible data
            passThru.ClearRxBuffer(channelId);

            // Finally we can send the message!
            PassThruMsg txMsg = new PassThruMsg(
                ProtocolID.ISO15765,
                TxFlag.ISO15765_FRAME_PAD,
                new byte[] { 0x00, 0x00, 0x07, 0xdf, 0x01, 0x00 });
            var txMsgPtr = txMsg.ToIntPtr();
            int numMsgs = 1;
            passThru.PassThruWriteMsgs(channelId, txMsgPtr, ref numMsgs, 50);
            
            // Read messages in a loop until we either timeout or we receive data
            numMsgs = 1;
            IntPtr rxMsgs = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PassThruMsg)) * numMsgs);
            J2534Err status = J2534Err.STATUS_NOERROR;
            while (J2534Err.STATUS_NOERROR == status)
                status = passThru.PassThruReadMsgs(channelId, rxMsgs, ref numMsgs, 200);

            // If we received data, we want to extract the data of interest.  I'm removing the reflection of the transmitted message.
            if ((J2534Err.ERR_BUFFER_EMPTY == status || J2534Err.ERR_TIMEOUT == status) && numMsgs > 0)
            {
                foreach (PassThruMsg msg in rxMsgs.AsList<PassThruMsg>(numMsgs))
                {
                    //
                    //
                    // Now do something with the data!
                    //
                    //
                }
            }


            // Disconnect this channel
            passThru.PassThruDisconnect(channelId);

            // When we are done with the device, we can free the library.
            passThru.FreeLibrary();
        }

        private void CmdReadVoltageClick(object sender, EventArgs e)
        {
            J2534Extended passThru = new J2534Extended(); ;// = Loader.Lib;
            double voltage = 0;

            if (!Connect()) return;

            if (!comm.GetBatteryVoltage(ref voltage))
            {
                MessageBox.Show(String.Format("Error reading voltage.  Error: {0}", comm.GetLastError()));
            }

            Disconnect();

            txtVoltage.Text = voltage + @" V";
        }

        private void ReadVinClick(object sender, EventArgs e)
        {
            string vin = "";

            try
            {
                J2534Extended passThru = new J2534Extended();
                
                if (!Connect()) return;

                vin = comm.GetVin();

            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error retrieving VIN due to OBD error: " + obdEx.Message);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error retrieving VIN due to UDS error: " + udsEx.Message);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error retrieving VIN due to J2534 error: " + j2534Ex.Message);
            } catch (Exception ex)
            {
                MessageBox.Show("Unknown error occured whilst retrieving VIN: " + ex.Message);
            }

            txtReadVin.Text = vin;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Assembly.GetExecutingAssembly().Location);
        }


        bool Connect()
        {
            try {
                LoadJ2534();
                comm = new UDSFord(passThru);
                comm.ConnectISO15765();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to make OBD ISO15765 connection due to: " + e.Message);
                return false;
            }
            return true;
        }

        bool LoadJ2534()
        {

            J2534Device j2534Device;

            // Find all of the installed J2534 passthru devices
            List<J2534Device> availableJ2534Devices = J2534Detect.ListDevices();
            if (availableJ2534Devices.Count == 0)
            {
                MessageBox.Show("Could not find any installed J2534 devices.");
                return false;
            }

            if (checkBoxLogJ2534.Checked)
            {
                j2534Device = new J2534Device();
                j2534Device.FunctionLibrary = System.IO.Directory.GetCurrentDirectory() + "\\" + "J2534DotNet.Logger.dll";
                Thread.Sleep(10);
                var loaded = passThru.LoadLibrary(j2534Device);
                return loaded;
            }

            //If there is only one DLL to choose from then load it
            if (availableJ2534Devices.Count == 1)
            {
                return passThru.LoadLibrary(availableJ2534Devices[0]);
            }
            else
            {
                var sd = new SelectDevice();
                if (sd.ShowDialog() == DialogResult.OK)
                {
                    j2534Device = sd.Device;
                    var loaded = passThru.LoadLibrary(j2534Device);
                    return loaded;
                }
            }

            return false;
        }

        void Disconnect()
        {
            if(comm != null) comm.Disconnect();
            if(passThru != null) passThru.FreeLibrary();

        }

        void UpdateLog(string text)
        {
            log.Text += text + Environment.NewLine;
        }



        BackgroundWorker flashReadBW;
        ProgressForm flashReadProgressForm;
        private void ReadFlash_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            flashReadProgressForm = new ProgressForm("Reading PCM flash...");

            flashReadBW = new BackgroundWorker();

            flashReadBW.WorkerReportsProgress = true;

            flashReadBW.DoWork += new DoWorkEventHandler(ReadFlash);
            flashReadBW.ProgressChanged += new ProgressChangedEventHandler(UpdateFlashReadProgress);
            flashReadBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FlashReadFinished);

            flashReadProgressForm.Owner = (Form)this.Parent;
            flashReadProgressForm.StartPosition = FormStartPosition.CenterScreen;
            flashReadProgressForm.Show();

            flashReadBW.RunWorkerAsync();
        }

        private void FlashReadFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            flashReadProgressForm.Close();
            this.Enabled = true;
            if(flashMemory != null) SaveFile(flashMemory);
        }

        private void UpdateFlashReadProgress(object sender, ProgressChangedEventArgs e)
        {
            flashReadProgressForm.UpdatePercentage(e.ProgressPercentage);
        }

        byte[] flashMemory;
        void ReadFlash(object sender, DoWorkEventArgs e)
        {
            //ReadFlashMemoryTest(flashReadBW);
            //return;

            try
            {
                if (!Connect()) return;
                float voltHigh = 15;
                float voltLow = 1;

                if(ignoreProrgammingVoltageCheckBox.Checked)
                {
                    voltHigh = -1;
                    voltLow = 1000;
                }
                //Ensure the programming voltage is 0v as the PCM needs to see a transition from 0 -> 18v
                float voltage = comm.ReadProgrammingVoltage();
                if (voltage > voltLow)
                {
                    voltage = comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, 0xFFFFFFFF);
                    if (voltage > voltLow)
                    {
                        MessageBox.Show("Failed to set programming voltage (pin 13) to 0 volts, measured: " + voltage + " V");
                        return;
                    }
                }

                voltage = comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, 18000);
                if (voltage < voltHigh)
                {
                    MessageBox.Show("Failed to set programming voltage (pin 13) to 18 volts, measured: " + voltage + " V");
                    return;
                }
                MessageBox.Show("Please turn the ignition off, wait 3 seconds, then turn it back on before pressing OK.");

                //Ensure the programming voltage is still high after an ignition cycle
                voltage = comm.ReadProgrammingVoltage();
                if (voltage < voltHigh)
                {
                    MessageBox.Show("Programming voltage did not persist after ignition power cycle), measured: " + voltage + " V");
                    return;
                }


                //Enter level 1 seecurity mode
                comm.SecurityAccess(0x01);

                flashMemory = comm.ReadFlashMemory(flashReadBW);

            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error entering reading flash due to OBD error: " + obdEx.Message, "Error", MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error entering reading flash due to UDS error: " + udsEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error entering security level due to J2534 error: " + j2534Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown error occured whilst entering security level: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            finally
            {
                Disconnect();
            }
        }

        public byte[] ReadFlashMemoryTest(BackgroundWorker progressReporter = null)
        {
            byte[] flashMemory = new byte[0x100000];
            for (uint i = 0x0; i <= 0xFF800; i += 0x800)
            {
                Thread.Sleep(10);

                //Report progress back to the GUI if there is one
                if (progressReporter != null) progressReporter.ReportProgress((int)((float)i / (float)0xFF800 * 100.0f));

            }
            return flashMemory;
        }

        void SaveFile(byte [] rawBinaryFile)
        {
            SaveFileDialog savefile = new SaveFileDialog();

            savefile.Filter = "Binary File|*.bin";

            if (savefile.ShowDialog() != DialogResult.OK) return;
            
            try
            {
                File.WriteAllBytes(savefile.FileName, rawBinaryFile);

                MessageBox.Show("Successfully Saved File!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
             
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            Disconnect();
        }

        private void checkBoxLogJ2534_CheckStateChanged(object sender, EventArgs e)
        {

        }

        bool toggle = false;
        private void setProgrammingVoltage(object sender, EventArgs e)
        {
            SetVoltage();
        }

        void SetVoltage(bool off = false)
        {
            try
            {
                if (!Connect()) return;

                if (comm == null) comm = new UDSFord(passThru);

                if (off)
                {
                    UpdateLog("setProgrammingVoltage(PinNumber.PIN_13, OFF)");
                    float programmingVoltage = comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, 0xFFFFFFFF);
                    UpdateLog("Voltage = : " + programmingVoltage);
                    toggle = false;
                }
                else
                {
                    uint volts = 0;
                    if (!UInt32.TryParse(textBoxVolts.Text, out volts)) return;

                    UpdateLog("setProgrammingVoltage(PinNumber.PIN_13 " + volts + " mV");
                    float programmingVoltage = comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, volts);
                    UpdateLog("Voltage = " + programmingVoltage + "V");
                    toggle = true;
                }

            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error setting voltage due to OBD error: " + obdEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error setting voltage due to UDS error: " + udsEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error setting voltage due to J2534 error: " + j2534Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown error setting voltage: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            finally
            {
                Disconnect();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SetVoltage(true);
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }



    }
}
