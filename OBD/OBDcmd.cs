using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OBD
{

    public class OBDcmd
    {
        public enum Mode
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

        public enum Reponse
        {
            SUCCESS = 0x40,
            NEGATIVE = 0x7F
        }
    }



 
}
