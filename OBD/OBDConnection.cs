#region License
/* 
 * Copyright (c) 2016, Roland Harrison
 * roland.c.harrison@gmail.com
 *
 * Copyright (c) 2010, Michael Kelly
 * michael.e.kelly@gmail.com
 * http://michael-kelly.com/
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
#endregion License
using System.Collections.Generic;
using System.Linq;
using System.Text;
using J2534DotNet;

namespace OBD
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class OBDConnection : OBDInterface
    {
        public IJ2534Extended j2534Interface;
        public ProtocolID protocolId;
        public int deviceId;
        public int channelId;
        public bool isConnected;
        public J2534Err m_status;

        public OBDConnection(IJ2534Extended j2534Interface)
        {
            this.j2534Interface = j2534Interface;
            isConnected = false;
            protocolId = ProtocolID.ISO15765;
            m_status = J2534Err.STATUS_NOERROR;
        }

        
        public string[] GetFaults()
        {
            byte[] data;

            ReadObdPid(OBDcmd.Mode.REQUEST_EMISSIONS_DTC, out data);
            if (data.Length > 0)
            {
                //TODO: PARSE DTCs  
            }

            return new string[0];

        }

        public bool ClearFaults()
        {
            byte[] value;
            ReadObdPid(OBDcmd.Mode.CLEAR_EMISSIONS_DTC, out value);

            //TODO
            //parse response

            return false;
        }


        public float PassThruSetProgrammingVoltage(PinNumber pinNumber, uint milliVolts)
        {

            if (milliVolts < 5000) milliVolts = 5000;
            if (milliVolts > 20000 && milliVolts != 0xFFFFFFFE && milliVolts != 0xFFFFFFFF) milliVolts = 20000;

            m_status = j2534Interface.PassThruSetProgrammingVoltage(deviceId, pinNumber, milliVolts);
            if (m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);

            Thread.Sleep(10);
            int mv = 0;
            m_status = j2534Interface.ReadProgrammingVoltage(deviceId, ref mv);
            if (m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);
            float voltage = ((float)mv) / 1000.0f;
            return voltage;
        }

        public float ReadProgrammingVoltage()
        {
            int mv = 0;
            m_status = j2534Interface.ReadProgrammingVoltage(deviceId, ref mv);
            if (m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);
            float voltage = ((float)mv) / 1000.0f;
            return voltage;
        }

        // Recursively read the available pids starting from 0x00 and inrementing by 0x20
        public void GetAvailableObdPidsAt(byte start, ref List<byte> availablePids)
        {
            //byte[] value;
            //// start = 0x00, 0x20, 0x40... 
            //if (ReadObdPid(0x01, start, out value))
            //{
            //    for (int i = 0; i < value.Length; i++)
            //    {
            //        for (int shift = 0; shift < 8; shift++)
            //        {
            //            byte mask = (byte)(0x80 >> shift);
            //            if ((value[i] & mask) != 0)
            //            {
            //                availablePids.Add((byte)((i * 0x8) + shift + 1 + start));
            //            }
            //        }
            //    }

            //    if (availablePids.Contains((byte)(start + 0x20)))
            //    {
            //        GetAvailableObdPidsAt((byte)(start + 0x20), ref availablePids);
            //    }
            //}
            //return;
        }

        public bool GetAvailableObdPids(ref List<byte> availablePids)
        {
            availablePids.Clear();
            GetAvailableObdPidsAt(0x00, ref availablePids);
            return (availablePids.Count > 0);
        }


        public bool GetBatteryVoltage(ref double voltage)
        {
            int millivolts = 0;
            m_status = j2534Interface.ReadBatteryVoltage(deviceId, ref millivolts);
            if (J2534Err.STATUS_NOERROR == m_status)
            {
                voltage = millivolts / 1000.0;
                return true;
            }
            return false;
        }

        public string GetVin()
        {
            string vin = "";
            byte[] value;
            ReadObdPid(OBDcmd.Mode.REQUEST_VEHICLE_INFORMATION, out value, (byte)PID.VehicleInformation.VIN);
            var vinArray = new byte[value.Length - 1];
            Array.Copy(value, 1, vinArray, 0, value.Length - 1);
            if (value.Length > 0) vin = Encoding.ASCII.GetString(vinArray.ToArray());
            return vin;
        }
        
        public bool ResetECU()
        {
            PassThruMsg txMsg = new PassThruMsg();
            int timeout;
            var value = new byte[0];

            txMsg.ProtocolID = protocolId;
            switch (protocolId)
            {
                case ProtocolID.ISO15765:
                    txMsg.TxFlags = TxFlag.ISO15765_FRAME_PAD;

                    txMsg.SetBytes(new byte[] { 0, 0, 0x07, 0xE0, 0x11, 0x02, 0, 0 });

                    timeout = 50;
                    break;
                case ProtocolID.J1850PWM:
                case ProtocolID.J1850VPW:
                case ProtocolID.ISO9141:
                case ProtocolID.ISO14230:
                default:
                    return false;
            }

            j2534Interface.ClearRxBuffer(channelId);

            int numMsgs = 1;
            m_status = j2534Interface.PassThruWriteMsgs(channelId, txMsg.ToIntPtr(), ref numMsgs, timeout);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }

            //Attempt to read at least 1 message as a reply
            List<PassThruMsg> messages;
            m_status = j2534Interface.ReadAllMessages(channelId, 1, timeout * 4, out messages);

            if (messages.Count <= 0)
            {
                return false;
            }
            var response1 = messages[0].GetBytes();
            var response2 = messages[1].GetBytes();   //needs to respond with 00 00 07 e8 67 03 xx xx xx
            var code = response2[6];
            if (response2[4] != 0x7F) return true;
            return false;
        }

        /// <summary>
        /// Returns a list of byte arrays with the tX messages
        /// </summary>
        /// <param name="replyBytes"></param>
        /// <returns></returns>
        public void ReadAllMessages(out List<PassThruMsg> messages, int msgNo = 10)
        {
            m_status = j2534Interface.ReadAllMessages(channelId, 1, 600, out messages, msgNo);

            if (m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);
        }

        public bool IsConnected()
        {
            return isConnected;
        }

        public void ConnectISO15765()
        {
            deviceId = 0;
            m_status = j2534Interface.PassThruOpen(IntPtr.Zero, ref deviceId);
            if (m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);

            byte[] value;
            m_status = j2534Interface.PassThruConnect(deviceId, ProtocolID.ISO15765, ConnectFlag.NONE, BaudRate.ISO15765, ref channelId);
            if (J2534Err.STATUS_NOERROR != m_status) throw new J2534Exception(m_status);

            //List<SConfig> configBits = new List<SConfig>();
            //SConfig conf = new SConfig();
            //conf.Parameter = ConfigParameter.LOOPBACK;
            //conf.Value = 0;
            //SConfig conf1 = new SConfig();
            //conf1.Parameter = ConfigParameter.DATA_RATE;
            //conf1.Value = 0;

            //configBits.Add(conf);
            //configBits.Add(conf1);
            //m_status = m_j2534Interface.GetConfig(m_channelId, ref configBits);


            int filterId = 0;

	        byte i;
	        
            for (i = 0; i < 1; i++)
            {
                PassThruMsg maskMsg = new PassThruMsg(ProtocolID.ISO15765, TxFlag.ISO15765_FRAME_PAD, new byte[] { 0xff, 0xff, 0xff, 0xff });
                PassThruMsg patternMsg = new PassThruMsg(ProtocolID.ISO15765, TxFlag.ISO15765_FRAME_PAD, new byte[] { 0x00, 0x00, 0x07, (byte)(0xE8 + i) });
                PassThruMsg flowControlMsg = new PassThruMsg(ProtocolID.ISO15765, TxFlag.ISO15765_FRAME_PAD, new byte[] { 0x00, 0x00, 0x07, (byte)(0xE0 + i) });

                m_status = j2534Interface.PassThruStartMsgFilter(
                    channelId,
                    FilterType.FLOW_CONTROL_FILTER,
                    maskMsg.ToIntPtr(),
                    patternMsg.ToIntPtr(),
                    flowControlMsg.ToIntPtr(),
                    ref filterId);

                if (J2534Err.STATUS_NOERROR != m_status)
                {
                    j2534Interface.PassThruDisconnect(channelId);
                    throw new J2534Exception(m_status);
                }
	        }
            
            //Check we can read some PIDs back
            ReadObdPid(OBDcmd.Mode.REQUEST_CURRENT_DATA, out value);
            if(value.Length <= 0)
            {
                m_status = j2534Interface.PassThruDisconnect(channelId);
                throw new OBDException(OBDcmd.Response.NEGATIVE_RESPONSE);
            }

            protocolId = ProtocolID.ISO15765;

        }

        public bool Disconnect()
        {
            m_status = j2534Interface.PassThruClose(deviceId);
            if (m_status != J2534Err.STATUS_NOERROR)
            {
                return false;
            }
            return true;
        }

        public string GetLastError()
        {
            return m_status.ToString();
        }


        public void SendMessage(byte[] payload, bool headerSuppled = false)
        {
            var txMsg = new PassThruMsg();
            int timeout;
            if (headerSuppled)
            {
                txMsg.SetBytes(payload);
            } else
            {
                //Use the standard header
                //First 4 bytes are 00 00 07 E8
                byte[] txMsgBytes = new byte[4 + payload.Length];
                txMsgBytes[2] = 0x07;
                txMsgBytes[3] = 0xE0;
                Array.Copy(payload, 0, txMsgBytes, 4, payload.Length);
                txMsg.SetBytes(txMsgBytes);
            }
            

            txMsg.ProtocolID = protocolId;


            if (protocolId != ProtocolID.ISO15765) throw new J2534Exception(J2534Err.ERR_NOT_SUPPORTED);

            txMsg.TxFlags = TxFlag.ISO15765_FRAME_PAD;
            timeout = 50;


            j2534Interface.ClearRxBuffer(channelId);

            var numMsgs = 1;
            m_status = j2534Interface.PassThruWriteMsgs(channelId, txMsg.ToIntPtr(), ref numMsgs, timeout);
            if (J2534Err.STATUS_NOERROR != m_status) throw new J2534Exception(m_status);

        }

        public void ReadObdPid(OBDcmd.Mode mode, out byte[] payload, byte pid = 0)
        {
            // See here for more details on this PID mode https://en.wikipedia.org/wiki/OBD-II_PIDs#Mode_1_PID_00
            int timeout = 50;
            payload = new byte[0];
            byte[] txMsgBytes;

            if ((byte)mode == 0x03 || (byte)mode == 0x04) txMsgBytes = new byte[] { 0x00, 0x00, 0x07, 0xdf, (byte)mode };
            else txMsgBytes = new byte[] { 0x00, 0x00, 0x07, 0xdf, (byte)mode, pid };

            SendMessage(txMsgBytes, true);

            //Attempt to read at least 1 message as a reply
            List<PassThruMsg> messages;
            ReadAllMessages(out messages);

            //bubble the error back to the user
            if (messages.Count <= 0) throw new J2534Exception(J2534Err.ERR_BUFFER_EMPTY);

            int index = GetStartOfMessageIndex(messages);

            //If there is no response return now, this is not necesarilly and error
            if (index == -1) return;

            ////Typical tx/rx response
            ////0x07 DF 09 02 (request VIN)
            ////0x07 E8 49 02 01 xx xx xx xx
            if (!ParseOBDResponse(messages[index], mode, out payload, pid)) throw new OBDException(OBDcmd.Response.NEGATIVE_RESPONSE);

        }

        /// <summary>
        /// Parse the replies checking for a valid response, if we have a valid response extract the payload data
        /// </summary>
        /// <param name="rxMsgs"></param>
        /// <param name="txMode"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        bool ParseOBDResponse(PassThruMsg rxMsg, OBDcmd.Mode txMode, out byte[] payload, byte pid = 0)
        {
            payload = new byte[0];
            var rxMsgBytes = rxMsg.GetBytes();

            //Iterate the reply bytes to find the echod ECU index, response code, function response and payload data if there is any
            //If we could use some kind of HEX regex this would be a bit neater
            int stateMachine = 0;
            for (int i = 0; i < rxMsgBytes.Length; i++)
            {
                switch (stateMachine)
                {
                    case 0:
                        if (rxMsgBytes[i] == 0x07) stateMachine = 1;
                        else if (rxMsgBytes[i] != 0) return false;
                        break;
                    case 1:
                        if (rxMsgBytes[i] == 0xE8)
                        {
                            stateMachine = 2;
                            //if (pid == 0) return true;
                        } else return false;
                        break;
                    case 2:
                        if (rxMsgBytes[i] == (byte)txMode + (byte)OBDcmd.Response.SUCCESS) stateMachine = 3;
                        else  return false; //This is an invalid response, give up now
                        break;
                    case 3:
                        //We have a positive response
                        if(pid != 0) //If a PID byte was sent it must be reflected
                        {
                            if (rxMsgBytes[i] != pid) return false;
                            i++; //payloads is after this byte
                        }
                        //return the payload if there is any
                        int payloadLength = rxMsgBytes.Length - i;
                        if (payloadLength > 0)
                        {
                            payload = new byte[payloadLength];
                            Array.Copy(rxMsgBytes, i, payload, 0, payloadLength);
                        }
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        public int GetStartOfMessageIndex(List<PassThruMsg> rxMsgs)
        {
            for (int i = 0; i < rxMsgs.Count; i++) if (rxMsgs[i].RxStatus == RxStatus.NONE) return i;
            return -1;

        }

        public byte[] RemoveNullPadding(byte[] bytes)
        {
            if (bytes == null) return null;
            if (bytes.Length <= 0) return null;
            int i;
            for (i = 0; i < bytes.Length; i++) if (bytes[i] != 0) break;
            var data = new byte[bytes.Length - i];
            for (int j = 0; j < bytes.Length - i; j++) data[j] = bytes[j + i];
            return data;
        }

    }


}
