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
using J2534DotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OBD
{
    /// <summary>
    /// Unified Diagnostic Services (UDS) is a vendor specific extension of OBD hence
    /// we literally extend our OBD implementation and implement the UDS interface
    /// This UDS implementation is via J2534
    /// </summary>
    public class UDSFord : OBDConnection, UDSInterface
    {
		#region details
		//The following commands are supported by the spanish oak PCM
		//0x10	Diagnostic Session Control	
		//0x11	ECU Reset	
		//		0x01 Hard Reset
		//		0x02 Key Off On Reset
		//		0x03 Soft Reset
		//0x14	Clear Diagnostic Information (not supported)
		//0x18	Read DTC By Status?
		//		02 FF FF (FF FF = group of DTC)
		//0x19	Read DTC Information (not supported)
		//0x22	Read Data By Identifier	
		//0x23	Read Memory By Address	
		//0x27	Security Access	
		//0x28	Communication Control (not supported)
		//0x2E	Write Data By Identifier (not supported)
		//0x2F	Input Output Control By Identifier (not supported)
		//0x31	Routine Control	
		//		0x01 Start Routine
		//		0x02 Stop Routine
		//		0x03 Request Routine Result
		//0x34	Request Download
		//0x35	Request Upload (not supported)
		//0x36	Transfer Data	
		//0x37	Transfer Exit (not supported)
		//0x3D	Write Memory By Address (not supported)
		//0x3E	Tester Present	
		//0x85	Control DTC Setting (not supported)
		//0xB1	DiagnosticCommand
		//		0x00 B2 AA 	- 	Erase Flash from address 0
		//		0x00 3C		-	Enable brakes
		//		0x00 2B		-	Disable brakes
		//		0x00 01 A3	-	Do magic??
		
		//https://automotive.wiki/index.php/ISO_14229
		
		#endregion details

        public UDSFord(IJ2534Extended j2534Interface) : base(j2534Interface)
        {
        }

        public byte[] ReadFlashMemory(BackgroundWorker progressReporter = null)
        {
            byte[] flashMemory = new byte[0x100000];
            byte[] buffer;
            uint blockSize = 0x800; //This is the largest size we can request that is an even divisible number, 0x900 is supported but then we need an odd request at the end
            for (uint i = 0x0; i <= 0xFF800; i+= blockSize)
            {
                ReadMemoryByAddress(i, blockSize, out buffer);

                //We recieved an incorrect amount of data, there is no way to handle this error so bubble it back to the user
                if (buffer.Length != blockSize) throw new UDSException(UDScmd.NegativeResponse.INCORRECT_MSG_LENGTH_OR_FORMAT); 

                Buffer.BlockCopy(buffer, 0, flashMemory, (int)i, buffer.Length);

                //Report progress back to the GUI if there is one
                if (progressReporter != null) progressReporter.ReportProgress((int)((float)i / (float)0xFF800 * 100.0f));
            }
            return flashMemory;
        }

        //This is used to erase the flash
        public void EraseFlash()
        {
            //Non Standard Manafacturer Specific Mode EraseFlash (Ford Spanish Oak)
            //byte1 ServiceID 0xB1
            //byte2 AddressAndLengthFormatIdentifier (0x00 for Ford Spanish Oak)
            //byte3 0xB2 magic byte 1
            //byte4 0xAA
            byte[] txMsgBytes = { (byte)UDScmd.Mode.DIAGNOSTIC_COMMAND, 0x00, 0xB2, 0xAA};
            SendMessage(txMsgBytes);

            List<PassThruMsg> rxMsgs;
            ReadAllMessages(out rxMsgs, 1, 1000, true);

            //Expect back 0x7F B1 78  (78=Response Pending)
            //0xF1 00 B2  which is success
        }

        public void RequestDownload()
        {
            //ISO14229 RequestDownload
            //byte1 ServiceID 0x34
            //byte2 DataFormatIdentifier 
            //      High nibble = memorySize)
            //      Low nibble  = memoryAddress
            //byte3 AddressAndLengthFormatIdentifier (0x01 for Ford Spanish Oak)
            //byte4 memoryAddressByte1
            //byte5 memoryAddressByte2
            //byte6 memoryAddressByte3
            //byte7 uncompressedMemorySizeByte1
            //byte8 uncompressedMemorySizeByte1
            //byte9 uncompressedMemorySizeByte1

            byte[] txMsgBytes = { (byte)UDScmd.Mode.REQUEST_DOWNLOAD, 0x00, 0x01, 0x00,00,00,00,0x30,00};
            SendMessage(txMsgBytes);

            //We expect 3 messages, rx, start of message, 0x78 (response pending) then the successful response
            List<PassThruMsg> rxMsgs;

            ReadAllMessages(out rxMsgs, 1, 100, true);

            //UDScmd.NegativeResponse subFunctionResponse = UDScmd.NegativeResponse.UNKNOWN;
            //byte[] payload;
            //bool success = false;
            //foreach(var msg in rxMsgs)
            //{
            //    UDSPacket rxPacket = ParseUDSResponse(msg, UDScmd.Mode.REQUEST_DOWNLOAD);

            //        success = true;
            //        break;
            //    }
            //}

            ////Bubble the last subFunctionError back to the user
            //if (!success) throw new UDSException(subFunctionResponse);

        }

        public void ReadMemoryByAddress(uint address, uint blockSize, out byte[] memory)
        {
            //Send the read memory request
            byte blockSizeUpper = (byte)((blockSize >> 8) & 0xFF);
            byte blockSizeLower = (byte)(blockSize & 0xFF);
            //ISO14229 ReadMemoryByAddress
            //byte1 ServiceID 0x23
            //byte2 AddressAndLengthFormatIdentifier (0 for Ford Spanish Oak)
            //byte3 address byte 1
            //byte4 address byte 2
            //byte5 address byte 3
            //byte6 address byte 4
            //byte7 block size byte1
            //byte8 block size byte2
            byte[] txMsgBytes = { (byte)UDScmd.Mode.READ_MEMORY_BY_ADDRESS, 0, (byte)((address >> 16) & 0xFF), (byte)((address >> 8) & 0xFF), (byte)((address) & 0xFF), blockSizeUpper, blockSizeLower};
            SendMessage(txMsgBytes);

            //We expect 3 messages, rx, start of message, then the payload data
            List<PassThruMsg> rxMsgs;

            //This will throw an exception if we don't get a valid reply
            while (ReadMessage(out rxMsgs, 250) == J2534Err.STATUS_NOERROR)
            {
                if (rxMsgs[0].RxStatus == RxStatus.NONE) break;
            }

            //If we couldn't find the start of the mesage give up
            if(m_status != J2534Err.STATUS_NOERROR) throw new J2534Exception(m_status);
            if (rxMsgs.Count < 1) throw new J2534Exception(J2534Err.ERR_INVALID_MSG);

            UDSPacket rxPacket = ParseUDSResponse(rxMsgs[0], UDScmd.Mode.READ_MEMORY_BY_ADDRESS);
            if (rxPacket.Response != UDScmd.Response.POSTIVE_RESONSE) {
                throw new UDSException(rxPacket.NegativeResponse);
            }
            memory = rxPacket.Payload;
        }

        public void SecurityAccess(byte subFunction)
        {
            //Send the security request
            byte[] txMsgBytes = {(byte)UDScmd.Mode.SECURITY_ACCESS, subFunction};
            SendMessage(txMsgBytes);

            //Attempt to read at least 1 message as a reply
            List<PassThruMsg> rxMsgs;
            ReadAllMessages(out rxMsgs,1, 200);

            //Find the start of the response and parse it.
            PassThruMsg seedKeyResponse;
            int startOfMessageIndex = GetStartOfMessageIndex(rxMsgs);

            if (startOfMessageIndex == -1) throw new J2534Exception(J2534Err.ERR_BUFFER_EMPTY);
            seedKeyResponse = rxMsgs[startOfMessageIndex];

            //needs to respond with 00 00 07 e8 67 03 xx xx xx
            //response is 00 00 7F 27 12 if you have just powered on and had VPP during power on but the command is incorrect length (unsupported)
            //response is 00 00 7F 27 11 if you have no VPP 
            //response is 00 07 E8 67 mode XX XX XX if success
            UDSPacket rxPacket = ParseUDSResponse(seedKeyResponse, UDScmd.Mode.SECURITY_ACCESS);
            if (rxPacket.Response == UDScmd.Response.NO_RESPONSE)
            {
                throw new UDSException(UDScmd.NegativeResponse.INCORRECT_MSG_LENGTH_OR_FORMAT);
            }
            else if (rxPacket.Response == UDScmd.Response.NEGATIVE_RESPONSE)
            { 
                //Inform the user of the error
                if(rxPacket.NegativeResponse == UDScmd.NegativeResponse.UNKNOWN)
                {
                    //no error code supplied, something else went wrong
                    throw new UDSException(UDScmd.NegativeResponse.INCORRECT_MSG_LENGTH_OR_FORMAT);
                }
                else
                {
                    //We got a sub function error code
                    throw new UDSException(rxPacket.NegativeResponse);
                }
            }
            else
            {
                if (rxPacket.Payload.Length < 3)
                {
                    //Incorrect seed response length
                    throw new UDSException(UDScmd.NegativeResponse.INCORRECT_MSG_LENGTH_OR_FORMAT);
                }
                else
                {
                    //Calculate the seed response
                    var seedresponse = CalculateResponseFromSeed(0x7E0, subFunction, rxPacket.Payload);

                    //Send the packet
                    txMsgBytes = new byte[] { (byte)UDScmd.Mode.SECURITY_ACCESS, (byte)(subFunction + 1), (byte)((seedresponse >> 16) & 0xFF), (byte)((seedresponse >> 8) & 0xFF), (byte)((seedresponse) & 0xFF) };
                    SendMessage(txMsgBytes);

                    //Attempt to read at least 1 message as a reply
                    ReadAllMessages(out rxMsgs,1,200);

                    //Get the response
                    startOfMessageIndex = GetStartOfMessageIndex(rxMsgs);
                    if (startOfMessageIndex == -1) throw new J2534Exception(J2534Err.ERR_BUFFER_EMPTY);

                    var unlockResponse = rxMsgs[startOfMessageIndex];
                    //needs to be 00 00 07 E8 67 04 (mode+1)  (or 67 02)

                    rxPacket = ParseUDSResponse(unlockResponse, UDScmd.Mode.SECURITY_ACCESS);
                    if (rxPacket.Response == UDScmd.Response.NO_RESPONSE)
                    {
                        throw new UDSException(UDScmd.NegativeResponse.INCORRECT_MSG_LENGTH_OR_FORMAT);
                    }
                    else if (rxPacket.Response == UDScmd.Response.NEGATIVE_RESPONSE)
                    {
                        //Inform the user of the error
                        if (rxPacket.NegativeResponse == UDScmd.NegativeResponse.UNKNOWN)
                        {
                            //no error code supplied, something else went wrong
                            throw new UDSException(UDScmd.NegativeResponse.INCORRECT_MSG_LENGTH_OR_FORMAT);
                        }
                        else
                        {
                            //We got a sub function error code
                            throw new UDSException(rxPacket.NegativeResponse);
                        }
                    }

                    if(rxPacket.SubFunction != subFunction + 1)
                    {
                        throw new Exception($"Returned an incorrect subfunction code, expected {subFunction+1} got {rxPacket.SubFunction}");
                    }

                    //We successfully entered the serurity level!

                }
            }

        }

        /// <summary>
        /// This routine was developed from "FordStuff.py" written by Chris Valasek and Charlie Miller
        /// See "Adventures in Automotive Networks and Control Units" for the full technical paper
        /// </summary>
        /// <param name="seedbyte1"></param>
        /// <param name="seedbyte2"></param>
        /// <param name="seedbyte3"></param>
        /// <returns></returns>
        private static int CalculateResponseFromSeed(int device, int mode, byte[] seedbytes)
        {
            int seed = (seedbytes[0] << 16) | (seedbytes[1] << 8) | seedbytes[2];

            byte[] secretKey;
            if (mode == 1)
            {
                if (SecretKeysLevel1.TryGetValue(device, out secretKey)) return GenerateSeedKeyResponse(seed, secretKey);
            }
            else
            {
                if (SecretKeysLevel2.TryGetValue(device, out secretKey)) return GenerateSeedKeyResponse(seed, secretKey);
            }

            return -1;
        }

        private static int GenerateSeedKeyResponse(int seed_int, byte[] secretKey)
        {
            byte s1 = secretKey[0];
            byte s2 = secretKey[1];
            byte s3 = secretKey[2];
            byte s4 = secretKey[3];
            byte s5 = secretKey[4];
            int mucked_value = 0xc541a9;

            int or_ed_seed = ((seed_int & 0xFF0000) >> 16) | (seed_int & 0xFF00) | (s1 << 24) | (seed_int & 0xff) << 16;

            int v8, v9, v10, v11, v12, v13, v14;
            for (int i = 0; i < 32; i++)
            {
                int a_bit = ((or_ed_seed >> i) & 1 ^ mucked_value & 1) << 23;
                v9 = v10 = v8 = a_bit | (mucked_value >> 1);
                mucked_value = v10 & 0xEF6FD7 | ((((v9 & 0x100000) >> 20) ^ ((v8 & 0x800000) >> 23)) << 20) | (((((mucked_value >> 1) & 0x8000) >> 15) ^ ((v8 & 0x800000) >> 23)) << 15) | (((((mucked_value >> 1) & 0x1000) >> 12) ^ ((v8 & 0x800000) >> 23)) << 12) | 32 * ((((mucked_value >> 1) & 0x20) >> 5) ^ ((v8 & 0x800000) >> 23)) | 8 * ((((mucked_value >> 1) & 8) >> 3) ^ ((v8 & 0x800000) >> 23));
            }

            for (int j = 0; j < 32; j++)
            {
                v11 = ((((s5 << 24) | (s4 << 16) | s2 | (s3 << 8)) >> j) & 1 ^ mucked_value & 1) << 23;
                v12 = v11 | (mucked_value >> 1);
                v13 = v11 | (mucked_value >> 1);
                v14 = v11 | (mucked_value >> 1);
                mucked_value = v14 & 0xEF6FD7 | ((((v13 & 0x100000) >> 20) ^ ((v12 & 0x800000) >> 23)) << 20) | (((((mucked_value >> 1) & 0x8000) >> 15) ^ ((v12 & 0x800000) >> 23)) << 15) | (((((mucked_value >> 1) & 0x1000) >> 12) ^ ((v12 & 0x800000) >> 23)) << 12) | 32 * ((((mucked_value >> 1) & 0x20) >> 5) ^ ((v12 & 0x800000) >> 23)) | 8 * ((((mucked_value >> 1) & 8) >> 3) ^ ((v12 & 0x800000) >> 23));
            }

            int key = ((mucked_value & 0xF0000) >> 16) | 16 * (mucked_value & 0xF) | ((((mucked_value & 0xF00000) >> 20) | ((mucked_value & 0xF000) >> 8)) << 8) | ((mucked_value & 0xFF0) >> 4 << 16);

            return key;
        }

        public void ECUReset(byte[] command)
        {
            throw new NotImplementedException();
        }

        public void ClearDiagnosticInformation(byte[] command)
        {
            throw new NotImplementedException();
        }



        public void RequestUpload()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parse the replies checking for a valid response, if we have a valid response extract the payload data
        /// </summary>
        /// <param name="rxMsgs"></param>
        /// <param name="txMode"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        UDSPacket ParseUDSResponse(PassThruMsg rxMsg, UDScmd.Mode txMode)
        {
            var rxMsgBytes = rxMsg.GetBytes();
            UDSPacket rxPacket = new UDSPacket();
            //Iterate the reply bytes to find the echod ECU index, response code, function response and payload data if there is any
            //If we could use some kind of HEX regex this would be a bit neater
            int stateMachine = 0;
            for (int i = 0; i < rxMsgBytes.Length; i++)
            {
                switch (stateMachine)
                {
                    case 0:
                        if (rxMsgBytes[i] == 0x07) stateMachine = 1;
                        else if (rxMsgBytes[i] != 0) return rxPacket;
                        break;
                    case 1:
                        if (rxMsgBytes[i] == 0xE8) stateMachine = 2;
                        else return rxPacket;
                        break;
                    case 2:
                        var payload = new byte[rxMsgBytes.Length - i];

                        int payloadLength = rxMsgBytes.Length - i;
                        if (payloadLength > 0)
                        {
                            payload = new byte[payloadLength];
                            Array.Copy(rxMsgBytes, i, payload, 0, payloadLength);
                            rxPacket = new UDSPacket(payload, txMode);
                            ;
                        }
                        return rxPacket;
                    case 3:
                    default:
                        return rxPacket;
                }
            }
            return rxPacket;
        }

        public class UDSPacket
        {
            public UDScmd.Response Response = UDScmd.Response.NO_RESPONSE;
            public UDScmd.NegativeResponse NegativeResponse = UDScmd.NegativeResponse.UNKNOWN;
            public UDScmd.Mode Mode = UDScmd.Mode.UNKNOWN;
            public byte SubFunction = 0;
            public byte[] Payload = new byte[0];

            public UDSPacket()
            {

            }

            public UDSPacket(byte [] data, UDScmd.Mode mode)
            {
                if (data == null) return;
                if (data.Length < 1) return;
                if((UDScmd.Response)data[0] != UDScmd.Response.NEGATIVE_RESPONSE) Response = (UDScmd.Response)(data[0] - (byte)mode);
                else Response =  UDScmd.Response.NEGATIVE_RESPONSE;
                if (data.Length < 2) return;
                Mode = (UDScmd.Mode)data[1];
                int payloadLength = 0;

                switch (mode)
                {
                    case UDScmd.Mode.READ_MEMORY_BY_ADDRESS:
                    case UDScmd.Mode.REQUEST_DOWNLOAD:
                        if (Response == UDScmd.Response.NEGATIVE_RESPONSE)
                        {
                            NegativeResponse = (UDScmd.NegativeResponse)data[2];
                            return;
                        }
                        payloadLength = data.Length - 1;
                        if (payloadLength < 1) return;
                        Payload = new byte[payloadLength];
                        Buffer.BlockCopy(data, 1, Payload, 0, payloadLength);
                        break;
                    case UDScmd.Mode.SECURITY_ACCESS:
                        int offset;
                        if (Response == UDScmd.Response.NEGATIVE_RESPONSE)
                        {
                            offset = 3;
                            if (data.Length < 3) return;
                            NegativeResponse = (UDScmd.NegativeResponse)data[2];
                        }
                        else
                        {
                            offset = 2;
                            SubFunction = data[1];
                        }
                        payloadLength = data.Length - offset;
                        if (payloadLength < 1) return;
                        Payload = new byte[payloadLength];
                        Buffer.BlockCopy(data, offset, Payload, 0, payloadLength);
                        break;
                    default:
                        break;
                }
            }
        }

        private static readonly Dictionary<int, byte[]> SecretKeysLevel1 = new Dictionary<int, byte[]>
        {
            {0x726, new byte[]{0x3F,0x9E,0x78,0xC5,0x96}},
            {0x727, new byte[]{0x50,0xC8,0x6A,0x49,0xF1}},
            {0x733, new byte[]{0xAA,0xBB,0xCC,0xDD,0xEE}},
            {0x736, new byte[]{0x08,0x30,0x61,0x55,0xAA}},
            {0x737, new byte[]{0x52,0x6F,0x77,0x61,0x6E}},
            {0x760, new byte[]{0x5B,0x41,0x74,0x65,0x7D}},
            {0x765, new byte[]{0x96,0xA2,0x3B,0x83,0x9B}},
            {0x7a6, new byte[]{0x50,0xC8,0x6A,0x49,0xF1}},
            {0x7e0, new byte[]{0x08,0x30,0x61,0xA4,0xC5}}
        };

        private static readonly Dictionary<int, byte[]> SecretKeysLevel2 = new Dictionary<int, byte[]>
        {
            {0x737, new byte[]{0x44,0x49,0x4F,0x44,0x45}},
            {0x7e0, new byte[]{0x5A,0x89,0xE4,0x41,0x72}}
        };
    }

    public class UDScmd
    {
        public enum Mode : byte
        {
            DIAGNOSTIC_SESSION_CONTROL = 0x10,
            ECU_RESET = 0x11,
            CLEAR_DIAGNOSTIC_INFORMATION = 0x14,
            READ_DTC_BY_STATUS = 0x18,
            READ_DTC_INFORMATION = 0x19,
            READ_DATA_BY_INDENTIFIER = 0x22,
            READ_MEMORY_BY_ADDRESS = 0x23,
            SECURITY_ACCESS = 0x27,
            COMMUNICATION_CONTROL = 0x28,
            WRITE_DATA_BY_IDENTIFIER = 0x2E,
            IO_CONTROL_BY_IDENTIFIER = 0x2F,
            ROUTINE_CONTROL = 0x31,
            REQUEST_DOWNLOAD = 0x34,
            REQUEST_UPLOAD = 0x35,
            TRANSFER_DATA = 0X36,
            TRANSFER_EXIT = 0X37,
            WRITE_MEMORY_BY_ADDRESS = 0X3D,
            TESTER_PRESENT = 0X3E,
            CONTROL_DTC_SETTING = 0X85,
            DIAGNOSTIC_COMMAND = 0xB1,
            UNKNOWN = 0xFF,
        }

        public enum Response : byte
        {
            NO_RESPONSE = 0,
            POSTIVE_RESONSE = 0x40,
            NEGATIVE_RESPONSE = 0x7F
        }

        public enum NegativeResponse : byte
        {
            POSITIVE_RESPONSE = 0X00,
            GENERAL_REJECT = 0X10,
            SERVICE_NOT_SUPPORTED = 0X11,
            SUBFUNCTION_NOT_SUPPORTED = 0X12,
            INCORRECT_MSG_LENGTH_OR_FORMAT = 0X13,
            RESPONSE_TOO_LONG = 0X14,
            BUSY = 0X21,
            CONDITIONS_NOT_CORRECT = 0X22,
            REQUEST_SEQUENCE_ERROR = 0X24,
            REQUEST_OUT_OF_RANGE = 0X31,
            SECURITY_ACCESS_DENIED = 0X33,
            INVALID_SECURITY_KEY = 0X35,
            SECURITY_ATTEMPS_EXCEEED = 0X36,
            REQUIRED_TIME_DELAY_NOT_EXPIRED = 0X37,
            UPLOAD_DOWNLOAD_NOT_ACCEPTED = 0X70,
            TRANSFER_DATA_SUSPENDED = 0X71,
            GENERAL_PROGRAMMING_FAILURE = 0X72,
            WRONG_BLOCK_SEQUENCE_COUNTER = 0X73,
            REPONSE_PENDING = 0X78,
            SUBFUNCTION_NOT_SUPPORTED_IN_ACTIVE_SESSION = 0X7E,
            SERVICE_NOT_SUPPORTED_IN_ACTIVE_SESSION = 0X7F,
            RPM_TOO_HIGH = 0X81,
            RPM_TOO_LOW = 0X82,
            ENGINE_IS_RUNNING = 0X83,
            ENGINE_IS_NOT_RUNNING = 0X84,
            ENGINE_RUN_TIME_TOO_LOW = 0X85,
            TEMPERATURE_TOO_HIGH = 0X86,
            TEMPERATURE_TOO_LOW = 0x87,
            VEHICLESPEED_TOO_HIGH = 0x88,
            VEHICLESPEED_TOO_LOW = 0x89,
            THROTTLE_PEDAL_TOO_HIGH = 0x8A,
            THROTTLE_PEDAL_TOOL_LOW = 0x8B,
            TRANSMISSION_RANGE_NOT_IN_NEUTRAL = 0x8C,
            TRANSMISSION_RANGE_NOT_IN_GEAR = 0x8D,
            BRAKE_PEDAL_NOT_PRESSED_OR_NOT_APPLIED = 0x8F,
            SHIFTER_LEVER_NOT_IN_PARK = 0x90,
            TORQUE_CONVERTER_CLUTCH_LOCKED = 0x91,
            VOLTAGE_TOO_HIGH = 0x92,
            VOLTAGE_TOO_LOW = 0x93,

            UNKNOWN = 0xFF,
        }
        public enum DTCStatusByte : byte
        {
            TEST_FAILED_THIS_OPERATION_CYCLE = 0x02,
            PENDING_DTC = 0x04,
            CONFIRMED_DTC = 0x08,
            TEST_NOT_COMPLETED_SINCE_LAST_CLEAR = 0x10,
            TEST_FAILED_SINCE_LAST_CLEAR = 0x20,
            TEST_NOT_COMPLETED_THIS_OPERATION_CYCLE = 0x40,
            WARNING_INDICATOR_REQUESTED = 0x80,

        }

    }
}
