using System;
using System.IO;

namespace WebSocketNotificationService.Helpers
{
    public static class StreamExtentions
    {
        public static void WriteInt(this Stream stream, int value, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            stream.WriteToStream(buffer, isLittleEndian);
        }

        public static void WriteULong(this Stream stream, ulong value, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            stream.WriteToStream(buffer, isLittleEndian);
        }

        public static void WriteLong(this Stream stream, long value, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            stream.WriteToStream(buffer, isLittleEndian);
        }

        public static void WriteUShort(this Stream stream, ushort value, bool isLittleEndian = false)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            stream.WriteToStream(buffer, isLittleEndian);
        }
        private static void WriteToStream(this Stream stream, byte[] buffer, bool isLittleEndian)
        {
            if (BitConverter.IsLittleEndian && !isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
