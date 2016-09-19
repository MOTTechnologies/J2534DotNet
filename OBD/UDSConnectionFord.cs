using J2534DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OBD
{
    /// <summary>
    /// Unified Diagnostic Services (UDS) is a vendor specific extension of OBD hence
    /// we literally extend our OBD implementation and implement the UDS interface
    /// This UDS implementation requires a J2534
    /// </summary>
    public class UDSConnectionFord : OBDConnection, UDSInterface
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

        public UDSConnectionFord(IJ2534Extended j2534Interface) : base(j2534Interface)
        {

        }

        public bool ReadMemoryByAddress(uint address, out byte[] memory)
        {
            throw new NotImplementedException();
        }

        public bool SecurityAccess(byte subFunction)
        {
            //TODO handle exceptions
            

            //Send the security request
            PassThruMsg txMessage;
            byte[] txMsgBytes = {(byte)UDScmd.Mode.SECURITY_ACCESS, subFunction };
            if (!SendMessage(txMsgBytes)) return false;

            //Attempt to read at least 1 message as a reply
            List<PassThruMsg> rxMsgs;
            if (!ReadAllMessages(out rxMsgs)) return false;

            //Find the start of the response and parse it.
            byte[] payload;
            UDScmd.Response subFunctionResponse;
            PassThruMsg seedKeyResponse;
            int startOfMessageIndex = GetStartOfMessageIndex(rxMsgs);
            if (startOfMessageIndex == -1) return false;
            seedKeyResponse = rxMsgs[startOfMessageIndex];

            //needs to respond with 00 00 07 e8 67 03 xx xx xx
            //response is 00 00 7F 27 12 if you have just powered on and had VPP during power on but the command is incorrect length (unsupported)
            //response is 00 00 7F 27 11 if you have no VPP 
            //response is 00 07 E8 67 mode XX XX XX if success
            if (!ParseUDSResponse(seedKeyResponse, UDScmd.Mode.SECURITY_ACCESS, subFunction, out subFunctionResponse, out payload))
            {
                //Inform the user of the error
                if(subFunctionResponse == UDScmd.Response.UNKNOWN)
                {
                    //no error code supplied, something else went wrong
                }
                else
                {
                    //We got a sub function error code
                }
                return false;
            }
            else
            {
                if (payload.Length < 3)
                {
                    //Incorrect seed response length
                    return false;
                }
                else
                {
                    //Calculate the seed response
                    var seedresponse = CalculateResponseFromSeed(0x7E0, subFunction, payload);

                    txMsgBytes = new byte[] { (byte)UDScmd.Mode.SECURITY_ACCESS, (byte)(subFunction + 1), (byte)((seedresponse >> 16) & 0xFF), (byte)((seedresponse >> 8) & 0xFF), (byte)((seedresponse) & 0xFF) };
                    PassThruMsg txMsg;
                    if (!SendMessage(txMsgBytes)) return false;

                    //Attempt to read at least 1 message as a reply
                    if (!ReadAllMessages(out rxMsgs)) return false;

                    //Get the response
                    startOfMessageIndex = GetStartOfMessageIndex(rxMsgs);
                    if (startOfMessageIndex == -1) return false;

                    //needs to be 00 00 07 E8 67 04 (mode+1)  (or 67 02)
                    if (!ParseUDSResponse(seedKeyResponse, UDScmd.Mode.SECURITY_ACCESS, subFunction, out subFunctionResponse, out payload))
                    {
                        //Inform the user of the error
                        return false;
                    }

                    //We successfully entered the serurity level!
                    return true;

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
            if (mode == 1) if (SecretKeysLevel1.TryGetValue(device, out secretKey)) return GenerateSeedKeyResponse(seed, secretKey);
                else if (SecretKeysLevel2.TryGetValue(device, out secretKey)) return GenerateSeedKeyResponse(seed, secretKey);

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

        public bool ECUReset(byte[] command)
        {
            throw new NotImplementedException();
        }

        public bool ClearDiagnosticInformation(byte[] command)
        {
            throw new NotImplementedException();
        }

        public bool RequestDownload()
        {
            throw new NotImplementedException();
        }

        public bool RequestUpload()
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
        bool ParseUDSResponse(PassThruMsg rxMsg, UDScmd.Mode txMode, byte txSubFunction, out UDScmd.Response functionResponse, out byte[] payload)
        {
            payload = new byte[0];
            functionResponse = UDScmd.Response.UNKNOWN;
            bool positiveReponse = false;
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
                        if (rxMsgBytes[i] == (byte)txMode + (byte)OBDcmd.Response.SUCCESS)
                        {
                            //Positive response to the requested mode
                            positiveReponse = true;
                        }
                        else if (rxMsgBytes[i] != (byte)OBDcmd.Response.NEGATIVE_RESPONSE)
                        {
                            //This is an invalid response, give up now
                            return false;
                        }
                        stateMachine = 3;
                        break;
                    case 3:
                        functionResponse = (UDScmd.Response)rxMsgBytes[i];
                        if (positiveReponse && rxMsgBytes[i] == txSubFunction)
                        {
                            //We have a positive response and a positive subfunction code (subfunction is reflected)
                            int payloadLength = rxMsgBytes.Length - i;
                            if (payloadLength > 0)
                            {
                                payload = new byte[payloadLength];
                                Array.Copy(rxMsgBytes, i, payload, 0, payloadLength);
                            }
                            return true;
                        }
                        else
                        {
                            //We had a positive response but a negative subfunction error
                            //we return the function error code so it can be relayed
                            return false;
                        }
                    default:
                        return false;
                }
            }
            return false;
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
        }

        public enum Response : byte
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
