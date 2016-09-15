using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OBD
{
    public interface UDSInterface
    {
        bool ReadMemoryByAddress(uint address, out byte[] memory);
        bool SecurityAccess(byte mode);
    }
}
