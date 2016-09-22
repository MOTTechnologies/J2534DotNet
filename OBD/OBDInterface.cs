#region License
/* 
 * Copyright (c) 2016, Roland Harrison
 * roland.c.harrison@gmail.com
 *
 * Copyright (c) 2010, Michael Kelly
 * michael.e.kelly@gmail.com
 * http://michael-kelly.com/ 
 *  
 * (c) 2015 kuzkok
 * kuzkok@gmail.com
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
using System.Linq;
using System.Text;

namespace OBD
{
    /// <summary>
    /// J2534 Specific OBD implementation
    /// </summary>
    public interface OBDInterface
    {
        bool IsConnected();
        bool Disconnect();
        void ConnectISO15765();

        void ReadObdPid(OBDcmd.Mode mode, out byte[] payload, byte pid = 0);

        void GetAvailableObdPidsAt(byte start, ref List<byte> availablePids);

        bool GetAvailableObdPids(ref List<byte> availablePids);

        bool ClearFaults();

        string GetVin();

        bool GetBatteryVoltage(ref double voltage);

        void SendMessage(byte [] txMsgBytes, bool useHeader);

    }

}

        

