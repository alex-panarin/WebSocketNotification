using System;
using System.IO;
using System.Text;
using UniversalNotificationClient.DataExchange;

namespace UniversalNotificationClient.WebSocketCoders
{
    internal static class NotificationEncoder
    {
        public static NotificationRequest Encode(this NotificationRequest request)
        {
            byte[] msg = Encoding.UTF8.GetBytes(request.Payload);

            if (request.Option == WebSocketOption.Handshake)
            {
                request.Bytes = msg;
                return request;
            }

            byte[] buffer = new byte[6 + msg.Length];

            using MemoryStream memoryStream = new MemoryStream(buffer);

            byte finBitSetAsByte = (byte)0x80;
            byte byte1 = (byte)(finBitSetAsByte | (byte)request.Option);
            memoryStream.WriteByte(byte1);

            // NOTE:  A client must mask any frames that it sends to the server.!!!
            // NB, set the mask flag if we are constructing a client frame
            byte maskBitSetAsByte = 0x80;

            // depending on the size of the length we want to write it as a byte, ushort or ulong
            if (msg.Length < 126)
            {
                byte byte2 = (byte)(maskBitSetAsByte | (byte)msg.Length);
                memoryStream.WriteByte(byte2);
            }
            else if (msg.Length <= ushort.MaxValue)
            {
                byte byte2 = (byte)(maskBitSetAsByte | 126);
                memoryStream.WriteByte(byte2);
                
                byte[] length = BitConverter.GetBytes((ushort)msg.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(length);
                }
                memoryStream.Write(length, 0, length.Length);
            }
            else
            {
                throw new NotSupportedException("Message length too big");
            }

            Span<byte> masks = new Span<byte>(new byte[4]);
            new Random().NextBytes(masks);

            memoryStream.Write(masks.ToArray(), 0, 4);

            for (int i = 0; i < msg.Length; ++i)
            {
                msg[i] = (byte)(msg[i] ^ masks[i % 4]);
            }

            memoryStream.Write(msg, 0, msg.Length);

            request.Bytes = buffer;

            return request;
        }
    }
}
