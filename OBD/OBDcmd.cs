using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OBD
{

    public class OBDcmd
    {
        public enum Mode : byte
        {
            REQUEST_CURRENT_DATA = 0x01,
            REQUEST_FREEZE_FRAME_DATA = 0x02,
            REQUEST_EMISSIONS_DTC = 0x03,
            CLEAR_EMISSIONS_DTC = 0x04,
            REQUEST_SPECIFIC_TEST_RESULTS = 0x06,
            REQUEST_TEST_RESULTS = 0x07,
            REQUEST_CONTROL_ONBOARD_DEVICE = 0x08,
            REQUEST_VEHICLE_INFORMATION = 0x09,
            REQUEST_PERMANENT_EMISSIONS_DTC = 0x0A
        }

        public enum Response : byte
        {
            SUCCESS = 0x40,
            NEGATIVE_RESPONSE = 0x7F,
            UNKNOWN = 0xFF
            
        }
    }

    public class PID
    {
        /// <summary>
        /// REQUEST_VEHICLE_INFORMATION = 0x09
        /// </summary>
        public enum VehicleInformation : byte
        {
            SUPPORTED_PIDS = 0x00,
            VIN_MESSAGE_COUNT = 0x01,
            VIN = 0x02,
            CALIBRATION_ID_MESSAGE_COUNT = 0x03,
            CALIBRATION_ID = 0x04,
            CVN_MESSAGE_COUNT = 0x05,
            CVN = 0x06,
            IN_USE_PERFORMANCE_TRACKING_IGNITION = 0x08,
            ECU_NAME_MESSAGE_COUNT = 0x09,
            ECU_NAME = 0x0A,
            IN_USE_PERFORMANCE_TRACKING_COMPRESSION = 0x0B

        }
    }
    



 
}
