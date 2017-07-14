using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;


namespace Common
{
    public class BinaryHelper
    {

        public byte[] rawBinary;

        public BinaryHelper(byte [] rawBinary)
        {
            this.rawBinary = rawBinary;
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ByteArrayToHexString(byte[] ba)
        {
            if (ba == null) return "";
            if (ba.Length <= 0) return "";

            string hex = "0x";
            foreach (byte b in ba)
                hex += String.Format("{0:X2} ", b);
            return hex.ToString();
        }

        public static string ByteArrayToHexString(byte[] ba, int offset, int length, bool spacing = true)
        {
            if (ba == null) return "";
            if (ba.Length <= 0) return "";
            int upperLimit = offset + length;
            if (upperLimit > ba.Length) upperLimit = ba.Length;

            string hex = "0x";
            for(int i = offset; i < upperLimit; i++)
            {
                hex += String.Format("{0:X2}", ba[i]);
                if (spacing) hex += " ";
            }
            return hex.ToString();
        }


        public static string GetString(int address, byte[] bytes, int maxLength = 40)
        {
            if (address + maxLength > bytes.Length) maxLength = (address + maxLength);

            byte[] buffer = new byte[maxLength];
            Array.Copy(bytes, address, buffer, 0, maxLength);
            return Encoding.ASCII.GetString(buffer);
        }


        /// <summary>
        /// Searches for a subset byte array and returns the index
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="bytesToFind"></param>
        /// <returns></returns>
        public static int FindBytes(byte [] rawBinary, byte[] needle, int searchLimit = int.MaxValue)
        {
            var len = needle.Length;
            var limit = rawBinary.Length - len < searchLimit ? rawBinary.Length - len : searchLimit;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    var val1 = needle[k];
                    var var2 = rawBinary[i + k];
                    if (val1 != var2) break;
                }
                if (k == len) return i;
            }
            return -1;
        }


        float[] floatCache;
        void InitialiseFloatCache()
        {
            floatCache = new float[rawBinary.Length];

            //Parallel.For(0, rawBinary.Length/4, i => {
            //    if (!TryGetBigEndianFloat((uint)i * 4, rawBinary, out floatCache[i * 4])) floatCache[i * 4] = float.NaN;
            //});
            for(int i = 0; i < rawBinary.Length/4; i++) { 
                if (!TryGetBigEndianFloat((uint)i * 4, rawBinary, out floatCache[i * 4])) floatCache[i * 4] = float.NaN;
            }

        }

        
        ///// <summary>
        ///// Searches for a subset byte array and returns the index
        ///// </summary>
        ///// <param name="bytes"></param>
        ///// <param name="bytesToFind"></param>
        ///// <returns></returns>
        //public List<Tuple<int,float>> FindFloats(float[] needle, float tolerance = 0.8f)
        //{
        //    if (floatCache == null) InitialiseFloatCache();
        //    var locations = new List<Tuple<int, float>>();
            
        //    var len = needle.Length* 4;
        //    var limit = rawBinary.Length - len;
        //    //float toleranceHigh = 1.0f + tolerance;
        //    //float toleranceLow = 1.0f - tolerance;
        //    for (uint i = 0; i <= limit; i+=4)
        //    {
        //        uint k = 0;
        //        float averageError = 0;
        //        for (; k < needle.Length; k++)
        //        {
        //            float value = floatCache[i+k*4];
        //            if (IsValidFloat(value))
        //            {
        //                if (!IsNumberWithinPercentOfNumber(value, tolerance, needle[k]))
        //                {
        //                    break;
        //                }
        //                averageError += Math.Abs(value - needle[k]);
        //            }
        //            else
        //            {
        //                break;
        //            }
                    
        //        }
        //        if (k == needle.Length)
        //        {
        //            averageError /= needle.Length;
        //            locations.Add(Tuple.Create((int)i, averageError));
        //            break;
        //        }
        //    }
        //    return locations;
        //}

        public bool VerifyFloats(float[] needle, uint loweraddress, uint upperaddress)
        {
            if (floatCache == null) InitialiseFloatCache();

            var len = needle.Length * 4;
            var limit = rawBinary.Length - len;

            if (upperaddress < limit) limit = (int)upperaddress;

            //Calculate the range of the data
            float range = Math.Abs(needle.Max() - needle.Min());

            float[] newData = new float[needle.Length];

            //float toleranceHigh = 1.0f + tolerance;
            //float toleranceLow = 1.0f - tolerance;
            for (uint i = loweraddress; i <= limit; i += 4)
            {
                uint k = 0;
                for (; k < needle.Length; k++)
                {
                    float value = floatCache[i + k * 4];
                    if (IsValidFloat(value))
                    {
                        newData[k] = value;
                    }
                    else
                    {
                        return false;
                    }
                }
                if (k >= needle.Length) break;
            }

            //Calculate the sum of squares
            double sumOfSquares = 0;
            for(int i = 0; i < needle.Length; i++)
            {
                sumOfSquares += Math.Pow((double)needle[i] - (double)newData[i], 2);
            }

            if (sumOfSquares > 2.0)
            {
                return false;
            }

            return true; 
        }


        public static bool TryGetString(int address, byte[] bytes, out string result, int maxLength = 40) { 
            result = "";
            if (address + maxLength > bytes.Length) return false;
            try
            {
                result = GetString(address, bytes, maxLength);
                return true;
            } catch { }
            return false;
        }


        public static uint GetBigEndianUInt32(uint address, byte[] bytes)
        {
            uint byte1 = (uint)bytes[(int)address] << 24;
            uint byte2 = (uint)bytes[(int)address + 1] << 16;
            uint byte3 = (uint)bytes[(int)address + 2] << 8;
            uint byte4 = (uint)bytes[(int)address + 3];
            return (byte1 + byte2 + byte3 + byte4);
        }

        public static int GetBigEndianInt32(uint address, byte[] bytes)
        {
            int byte1 = (int)bytes[(int)address] << 24;
            int byte2 = (int)bytes[(int)address + 1] << 16;
            int byte3 = (int)bytes[(int)address + 2] << 8;
            int byte4 = (int)bytes[(int)address + 3];
            return (byte1 + byte2 + byte3 + byte4);
        }

        public static uint GetSmallEndianUInt32(uint address, byte[] bytes)
        {
            uint byte1 = (uint)bytes[(int)address];
            uint byte2 = (uint)bytes[(int)address + 1] << 8;
            uint byte3 = (uint)bytes[(int)address + 2] << 16;
            uint byte4 = (uint)bytes[(int)address + 3] << 24;
            return (byte1 + byte2 + byte3 + byte4);
        }

        public static ushort GetBigEndianUInt16(uint address, byte[] bytes)
        {
            ushort byte1 = (ushort)(bytes[(int)address + 2] << 8);
            ushort byte2 = (ushort)bytes[(int)address + 1];
            return (ushort)(byte1 + byte2);
        }

        public static short GetBigEndianInt16(uint address, byte[] bytes)
        {
            ushort byte1 = (ushort)(bytes[(int)address + 2] << 8);
            ushort byte2 = (ushort)bytes[(int)address + 1];
            return (short)(byte1 + byte2);
        }

        /// <summary>
        /// Returns the next pointer in the byte array
        /// </summary>
        /// <param name="pointer"></param>
        public bool TryGetNextPointer(uint pointer, out uint nextPointer, bool [] indirectPointerAtThisLocation = null)
        {
            nextPointer = 0;
            if (rawBinary == null) return false;
            if (cache == null) GeneratePointerCache(true);
            if (cache.Length != rawBinary.Length) GeneratePointerCache(true);
            if(indirectPointerAtThisLocation.Length != cache.Length) throw new ArgumentException("IndirectPointer array is the wrong size! This is a bug.");

            //Align the pointer on a 4 byte boundary
            pointer += (pointer % 4);

            //Start the search at the current address and look for the next address that has a pointer referencing it.
            //We also passed in an array of indirect pointers to search (pointerAtThisLocation
            for (uint i = pointer+4; i < cache.Length; i+= 4)
            {
                if (cache[i].Count > 0 || indirectPointerAtThisLocation[i])
                {
                    nextPointer = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a cache of all pointers found and their memory location
        /// Aligned = only calculate on 4 byte boundaries
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="aligned"></param>
        static bool cacheAligned = true;
        static List<uint>[] cache;
        public void GeneratePointerCache(bool aligned = true)
        {
            if (rawBinary == null) return;
            cache = new List<uint>[rawBinary.Length];

            uint offset = 1;
            if (aligned) offset = 4;

            for (int i = 0; i < rawBinary.Length; i++) cache[i] = new List<uint>();

            for (uint i = 0; i < rawBinary.Length - 5; i+= offset)
            {
                uint value = GetBigEndianUInt32(i, rawBinary);
                if(value < rawBinary.Length)
                    cache[value].Add(i);
            }
        }

        /// <summary>
        /// All pointers are stored in a cache so save searching the byte array each time.
        /// Aligned - Search for uint32s aligned at 4 byte indexs
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bytes"></param>
        /// <param name="aligned"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public List<uint> FindUint32(uint value, bool aligned, bool useCache = true)
        {
            if (rawBinary == null) return null;
            if (useCache)
            {
                if (cache == null) GeneratePointerCache(aligned);
                if (cacheAligned != aligned) GeneratePointerCache(aligned);
                if (cache.Length != rawBinary.Length) GeneratePointerCache(aligned);
                if (value < cache.Length) return cache[value];
            }

            List <uint> addresses = new List<uint>();
            uint offset = 1;
            if (aligned) offset = 4;
            for(uint address = 0; address + offset < rawBinary.Length; address += offset) if (GetBigEndianUInt32(address, rawBinary) == value) addresses.Add(address);

            return addresses;
        }

        public static bool TryGetByte(uint address, byte[] bytes, out byte value)
        {
            value = 0;
            if (bytes == null) return false;
            if (address > bytes.Length) return false;
            value = bytes[address];
            return true;
        }

        public static bool TryGetUInt32(uint address, byte[] bytes, out uint value)
        {
            value = 0;
            if (bytes == null) return false;
            if (address + 4 > bytes.Length) return false;
            value = GetBigEndianUInt32(address, bytes);
            return true;
        }

        public static bool TryGetInt32(uint address, byte[] bytes, out int value)
        {
            value = 0;
            if (bytes == null) return false;
            if (address + 4 > bytes.Length) return false;
            value = GetBigEndianInt32(address, bytes);
            return true;
        }

        public static bool TryGetUInt16(uint address, byte[] bytes, out ushort value)
        {
            value = 0;
            if (bytes == null) return false;
            if (address + 4 > bytes.Length) return false;
            value = GetBigEndianUInt16(address, bytes);
            return true;
        }

        public static bool TryGetInt16(uint address, byte[] bytes, out short value)
        {
            value = 0;
            if (bytes == null) return false;
            if (address + 4 > bytes.Length) return false;
            value = GetBigEndianInt16(address, bytes);
            return true;
        }

        public float GetBigEndianFloatFromCache(uint address)
        {
            if(floatCache == null) InitialiseFloatCache();
            return floatCache[address];
        }

            

        public static bool TryGetBigEndianFloat(uint address, byte[] bytes, out float value)
        {
            value = float.NaN;
            if (bytes == null) return false;
            if (address + 4 > bytes.Length) return false;

            try
            {
                byte[] data = new byte[4];
                data[0] = bytes[address + 3];
                data[1] = bytes[address + 2];
                data[2] = bytes[address + 1];
                data[3] = bytes[address];
                value = BitConverter.ToSingle(data, 0);
            }
            catch { return false; }


            return IsValidFloat(value);
        }

        public static bool TryGetSmallEndianFloat(uint address, byte[] bytes, out float value)
        {
            value = float.NaN;
            if (bytes == null) return false;
            if (address + 4 > bytes.Length) return false;

            try
            {
                byte[] data = new byte[4];
                data[0] = bytes[address];
                data[1] = bytes[address + 1];
                data[2] = bytes[address + 2];
                data[3] = bytes[address + 3];
                value = BitConverter.ToSingle(data, 0);
            }
            catch { return false; }


            return IsValidFloat(value);
        }

        public static bool TryGetBigEndianDouble(uint address, byte[] bytes, out double value)
        {
            value = double.NaN;
            if (bytes == null) return false;
            if (address + 8 > bytes.Length) return false;

            try { value = BitConverter.ToDouble(bytes, (int)address); }
            catch { return false; }

            return IsValidDouble(value);
        }

        public static bool IsValidFloat(float value)
        {
            return (!(float.IsNaN(value) || float.IsInfinity(value) || float.IsNegativeInfinity(value) || float.IsPositiveInfinity(value)));
        }

        public static bool IsValidDouble(double value)
        {
            return (!(double.IsNaN(value) || double.IsInfinity(value) || double.IsNegativeInfinity(value) || double.IsPositiveInfinity(value)));
        }
        /// <summary>
        /// Writes a float to the binary file
        /// TODO: Check we are writing on a correct boundary
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <param name="rawBinary"></param>
        /// <returns></returns>
        public static bool WriteFloat(uint address, float value, byte [] rawBinary)
        {
            if (rawBinary == null) return false;
            if (!BinaryHelper.IsValidFloat(value)) return false;

            if (address > rawBinary.Length + 4) return false;

            var floatBytes = BitConverter.GetBytes(value);
            Array.Reverse(floatBytes);
            try
            {
                Array.Copy(floatBytes, 0, rawBinary, address, floatBytes.Length);
                return true;
            }
            catch { }

            return false;
        }
        /// <summary>
        /// Writes a double to the binary file
        /// TODO: Check to make sure we are writing on a correct boundary
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <param name="rawBinary"></param>
        /// <returns></returns>
        public static bool WriteDouble(uint address, double value, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (!BinaryHelper.IsValidDouble(value)) return false;

            if (address > rawBinary.Length + 8) return false;

            var doubleBytes = BitConverter.GetBytes(value);
            Array.Reverse(doubleBytes);

            try
            {
                Array.Copy(doubleBytes, 0, rawBinary, address, doubleBytes.Length);
                return true;
            }
            catch { }

            return false;
        }

        public static bool WriteByte(uint address, byte value, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address > rawBinary.Length) return false;

            rawBinary[address] = value;
            return true;
        }

        public static bool WriteBoolean(uint address, bool value, uint mask, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address > rawBinary.Length) return false;

            byte byteValue = (value) ? (byte)1 : (byte)0;
            if (mask == 0) rawBinary[address] = byteValue;
            else rawBinary[address] = (byte)(byteValue << (byte)mask);
            return true;
        }

        public static bool WriteMaskedByte(uint address, byte value, uint mask, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address > rawBinary.Length) return false;

            if (mask == 0) rawBinary[address] = value;
            else rawBinary[address] = (byte)(value << (byte)mask);
            return true;
        }

        public static bool WriteMaskedUInt16(uint address, ushort value, uint mask, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address > rawBinary.Length) return false;

            if (mask == 0) return WriteBigEndianUInt32(address, value, rawBinary);
            else {
                ushort maskedShort = (ushort)(value << (ushort)mask);
                return WriteBigEndianUInt32(address, maskedShort, rawBinary);
            }
        }

        public static bool WriteMaskedInt16(uint address, short value, uint mask, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address > rawBinary.Length) return false;

            if (mask == 0) return WriteBigEndianInt16(address, value, rawBinary);
            else {
                short maskedValue = (short)(value << (short)mask);
                return WriteBigEndianInt32(address, maskedValue, rawBinary);
            }
        }

        public static bool WriteMaskedInt32(uint address, int value, uint mask, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address > rawBinary.Length) return false;

            if (mask == 0) return WriteBigEndianInt32(address, value, rawBinary);
            else {
                int maskedValue = (int)(value << (int)mask);
                return WriteBigEndianInt32(address, maskedValue, rawBinary);
            }
        }

        public static bool WriteMaskedUInt32(uint address, uint value, uint mask, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address > rawBinary.Length) return false;

            if (mask == 0) return WriteBigEndianUInt32(address, value, rawBinary);
            else {
                uint maskedValue = (uint)(value << (int)mask);
                return WriteBigEndianUInt32(address, maskedValue, rawBinary);
            }
        }



        public static bool WriteBigEndianUInt16(uint address, ushort value, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address + 2 > rawBinary.Length) return false;
            byte byte1 = (byte)(value >> 8);
            byte byte2 = (byte)(value);
            rawBinary[address] = byte1;
            rawBinary[address + 1] = byte2;
            return true;
        }

        public static bool WriteBigEndianInt16(uint address, short value, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address + 2 > rawBinary.Length) return false;
            byte byte1 = (byte)(value >> 8);
            byte byte2 = (byte)(value);
            rawBinary[address] = byte1;
            rawBinary[address + 1] = byte2;
            return true;
        }

        public static bool WriteBigEndianUInt32(uint address, uint value, byte[] rawBinary)
        {
            if (rawBinary == null) return false;
            if (address + 4 > rawBinary.Length) return false;
            byte byte1 = (byte)(value >> 24);
            byte byte2 = (byte)(value >> 16);
            byte byte3 = (byte)(value >> 8);
            byte byte4 = (byte)(value);
            rawBinary[address] = byte1;
            rawBinary[address + 1] = byte2;
            rawBinary[address + 2] = byte3;
            rawBinary[address + 3] = byte4;
            return true;
        }

        public static bool WriteBigEndianInt32(uint address, int value, byte[] bytes)
        {
            if (address + 4 > bytes.Length) return false;
            byte byte1 = (byte)(value >> 24);
            byte byte2 = (byte)(value >> 16);
            byte byte3 = (byte)(value >> 8);
            byte byte4 = (byte)(value);
            bytes[address] = byte1;
            bytes[address + 1] = byte2;
            bytes[address + 2] = byte3;
            bytes[address + 3] = byte4;
            return true;
        }

        public static int GetSizeOf(String type)
        {
            if (type.Equals("System.Double")) return 8;
            else if (type.Equals("System.Single")) return 4;
            else if (type.Equals("System.Int32")) return 4;
            else if (type.Equals("System.UInt32")) return 4;
            else if (type.Equals("System.Int16")) return 2;
            else if (type.Equals("System.UInt16")) return 2;
            else if (type.Equals("System.Byte")) return 1;

            throw new NotImplementedException("Type: " + type + " PPC size is unknown");
        }

        public static uint GetUInt32FromHexString(string value)
        {
            if(string.IsNullOrEmpty(value)) return 0;
            return UInt32.Parse(value.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
        }

        public static byte GetByteFromHexString(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return Byte.Parse(value.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
        }

        public static bool TryGetUInt32FromHexString(string stringValue, out uint value)
        {
            ///FIXME this is inefficient
            try
            {
                value = GetUInt32FromHexString(stringValue);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        public static bool TryGetByteFromHexString(string stringValue, out byte value)
        {
            ///FIXME this is inefficient
            try
            {
                value = GetByteFromHexString(stringValue);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        public static bool ReadAndVerifyFloats(uint address, int length, byte[] rawBinary)
        {
            for (uint i = 0; i < length; i++)
            {
                float value;
                if (TryGetBigEndianFloat((uint)(address + (i * 4u)), rawBinary, out value))
                {
                    if (!IsValidFloat(value)) return false;
                    if (value > 65536.0 || value < -65536.0 || (value > 0.0 && value < 0.000001) || (value < 0.0 && value > -0.000001)) return false;
                }
                else return false;
            }
            return true;
        }

        public static bool ReadAndVerifyDoubles(uint address, int length, byte[] rawBinary)
        {
            for (uint i = 0; i < length; i++)
            {
                double value;
                if (TryGetBigEndianDouble((uint)(address + (i * 8u)), rawBinary, out value))
                {
                    if (!IsValidDouble(value)) return false;
                    if (value > 65536.0 || value < -65536.0 || (value > 0.0 && value < 0.000001) || (value < 0.0 && value > -0.000001)) return false;
                }
                else return false;
            }
            return true;
        }
    }
}
