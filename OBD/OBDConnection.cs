#region Copyright (c) 2010, Michael Kelly
/* 
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
        public IJ2534Extended m_j2534Interface;
        public ProtocolID protocolId;
        public int m_deviceId;
        public int m_channelId;
        public bool m_isConnected;
        public J2534Err m_status;

        public OBDConnection(IJ2534Extended j2534Interface)
        {
            m_j2534Interface = j2534Interface;
            m_isConnected = false;
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


        public float PassThruSetProgrammingVoltage(PinNumber pinNumber, long milliVolts)
        {
            if (milliVolts < 5000) milliVolts = 5000;
            if (milliVolts > 20000 && milliVolts < 0xFFFFFFFE ) milliVolts = 20000;

            m_status = m_j2534Interface.PassThruSetProgrammingVoltage(m_deviceId, pinNumber, milliVolts);
            if (m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);

            Thread.Sleep(10);
            int mv = 0;
            m_status = m_j2534Interface.ReadProgrammingVoltage(m_deviceId, ref mv);
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
            m_status = m_j2534Interface.ReadBatteryVoltage(m_deviceId, ref millivolts);
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

            if (value.Length > 0) vin = Encoding.ASCII.GetString(value.ToArray());
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

            m_j2534Interface.ClearRxBuffer(m_channelId);

            int numMsgs = 1;
            m_status = m_j2534Interface.PassThruWriteMsgs(m_channelId, txMsg.ToIntPtr(), ref numMsgs, timeout);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }

            //Attempt to read at least 1 message as a reply
            List<PassThruMsg> messages;
            m_status = m_j2534Interface.ReadAllMessages(m_channelId, 1, timeout * 4, out messages);

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
        public bool ReadAllMessages(out List<PassThruMsg> messages)
        {
            m_status = m_j2534Interface.ReadAllMessages(m_channelId, 1, 200, out messages);

            if( m_status != J2534Err.STATUS_NOERROR) return false;
            if (messages.Count <= 0) return false;

            return true;
        }

            //0x18: "readDiagnosticTroubleCodesByStatus"}



        public bool IsConnected()
        {
            return m_isConnected;
        }

        public bool DetectProtocol()
        {
            // possible return values:
            //  ProtocolID.ISO15765; // CAN
            //  ProtocolID.ISO9141;  // ISO-K
            //  ProtocolID.J1850PWM;  // J1850PWM
            //  ProtocolID.J1850VPW;  // J1850VPW

            m_deviceId = 0;
            m_status = m_j2534Interface.PassThruOpen(IntPtr.Zero, ref m_deviceId);
            if (m_status != J2534Err.STATUS_NOERROR)
                return false;
            if (ConnectIso15765())
            {
                protocolId = ProtocolID.ISO15765;
                m_isConnected = true;
            }
            return true;
        }

        public bool ConnectIso15765()
        {
            byte[] value;
            m_status = m_j2534Interface.PassThruConnect(m_deviceId, ProtocolID.ISO15765, ConnectFlag.NONE, BaudRate.ISO15765, ref m_channelId);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return false;
            }

            int filterId = 0;

	        byte i;
	        
            for (i = 0; i < 1; i++)
            {
                PassThruMsg maskMsg = new PassThruMsg(ProtocolID.ISO15765, TxFlag.ISO15765_FRAME_PAD, new byte[] { 0xff, 0xff, 0xff, 0xff });
                PassThruMsg patternMsg = new PassThruMsg(ProtocolID.ISO15765, TxFlag.ISO15765_FRAME_PAD, new byte[] { 0x00, 0x00, 0x07, (byte)(0xE8 + i) });
                PassThruMsg flowControlMsg = new PassThruMsg(ProtocolID.ISO15765, TxFlag.ISO15765_FRAME_PAD, new byte[] { 0x00, 0x00, 0x07, (byte)(0xE0 + i) });

                m_status = m_j2534Interface.PassThruStartMsgFilter(
                    m_channelId,
                    FilterType.FLOW_CONTROL_FILTER,
                    maskMsg.ToIntPtr(),
                    patternMsg.ToIntPtr(),
                    flowControlMsg.ToIntPtr(),
                    ref filterId);

                if (J2534Err.STATUS_NOERROR != m_status)
                {
                    m_j2534Interface.PassThruDisconnect(m_channelId);
                    return false;
                }
	        }
            
            //Check we can read some PIDs back
            ReadObdPid(OBDcmd.Mode.REQUEST_CURRENT_DATA, out value);
            if(value.Length <= 0)
            {
                m_status = m_j2534Interface.PassThruDisconnect(m_channelId);
                return false;
            }
             

            return true;
        }

        public bool Disconnect()
        {
            m_status = m_j2534Interface.PassThruClose(m_deviceId);
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


        public bool SendMessage(byte[] payload)
        {
            var txMsg = new PassThruMsg();
            int timeout;

            //First 4 bytes are 00 00 07 E8
            byte []txMsgBytes = new byte[4 + payload.Length];
            txMsgBytes[2] = 0x07;
            txMsgBytes[3] = 0x08;
            Array.Copy(payload, 0, txMsgBytes, 4, payload.Length);

            txMsg.ProtocolID = protocolId;
            txMsg.SetBytes(txMsgBytes);

            if (protocolId != ProtocolID.ISO15765) return false;

            txMsg.TxFlags = TxFlag.ISO15765_FRAME_PAD;
            timeout = 50;


            m_j2534Interface.ClearRxBuffer(m_channelId);

            var numMsgs = 1;
            m_status = m_j2534Interface.PassThruWriteMsgs(m_channelId, txMsg.ToIntPtr(), ref numMsgs, timeout);
            if (J2534Err.STATUS_NOERROR != m_status) return false;

            return true;
        }

        public void ReadObdPid(OBDcmd.Mode mode, out byte[] payload, byte pid = 0)
        {
            // See here for more details on this PID mode https://en.wikipedia.org/wiki/OBD-II_PIDs#Mode_1_PID_00
            int timeout = 50;
            payload = new byte[0];
            byte[] txMsgBytes;

            if ((byte)mode == 0x03 || (byte)mode == 0x04) txMsgBytes = new byte[] { 0x00, 0x00, 0x07, 0xdf, (byte)mode };
            else txMsgBytes = new byte[] { 0x00, 0x00, 0x07, 0xdf, (byte)mode, pid };

            //if the message failed to send bubble the error back
            if (!SendMessage(txMsgBytes)) throw new J2534Exception(m_status);

            //Attempt to read at least 1 message as a reply
            List<PassThruMsg> messages;
            m_status = m_j2534Interface.ReadAllMessages(m_channelId, 1, timeout * 4, out messages);

            //bubble the error back to the user
            if (m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);

            int index = GetStartOfMessageIndex(messages);

            //If there is no response throw a timeout exception
            if(index == -1) throw new J2534Exception(J2534Err.ERR_TIMEOUT);

            //Throw an OBD exception if we got an error

            ////Typical tx/rx response
            ////0x07 DF 09 02 (request VIN)
            ////0x07 E8 49 02 01 xx xx xx xx
            if (!ParseOBDResponse(messages[index], mode, out payload, pid)) throw new OBDException(OBDcmd.Response.NEGATIVE_RESPONSE);

            ////Check we got a successfull response
            //response = RemoveNullPadding(response);

            ////Only accept 0x7E8,0x7E9,0x7E8,0x7EA etc
            //if (response[0] != 0x07) return false;
            //var temp = (response[1] & 0xE0);
            //if (temp != 0xE0) return false;

            ////Check the response code was valid
            //if (response[2] != 0x40 + (byte)mode) return false;

            ////Check the PID recieved was the one requested
            //if (response[3] != pid) return false;

            //var valueLength = response.Length - 5;
            //if (valueLength <= 0) return false;

            //value = new byte[response.Length - 5];

            ////Remove the response from the reply
            //Array.Copy(response, 5, value, 0, response.Length - 5);
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
                        if (rxMsgBytes[i] == 0xE8) stateMachine = 2;
                        return false;
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
            for (int i = 0; i < rxMsgs.Count; i++) if (rxMsgs[i].RxStatus == RxStatus.START_OF_MESSAGE) return i;
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
