using System;
using System.IO;

namespace StockExchangeNotificationService.Helpers
{
    public class WSHelper
    {
        public static void WriteInt(int value, Stream stream, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteToStream(buffer, stream, isLittleEndian);
        }

        public static void WriteULong(ulong value, Stream stream, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteToStream(buffer, stream, isLittleEndian);
        }

        public static void WriteLong(long value, Stream stream, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteToStream(buffer, stream, isLittleEndian);
        }

        public static void WriteUShort(ushort value, Stream stream, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteToStream(buffer, stream, isLittleEndian);
        }
        private static void WriteToStream(byte[] buffer, Stream stream, bool isLittleEndian)
        {
            if (BitConverter.IsLittleEndian && !isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
