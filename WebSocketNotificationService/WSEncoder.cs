﻿using StockExchangeNotificationService.Helpers;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;

namespace StockExchangeNotificationService
{
    public partial class WSEncoder : IWSEncoder
    {
        public WSEncoder()
        {
        }
        public byte[] EncodeMessage(WebSocketMessageTypeEx messageType, string message, bool isLastFrame = true)
        {
            byte[] msg = Encoding.UTF8.GetBytes(message);

            byte[] buffer = new byte[6 + msg.Length];

            using MemoryStream memoryStream = new MemoryStream(buffer);

            byte finBitSetAsByte = isLastFrame ? (byte)0x80 : (byte)0x00;
            byte byte1 = (byte)(finBitSetAsByte | GetOption(messageType));
            memoryStream.WriteByte(byte1);

            // NOTE:  A server must not mask any frames that it sends to the client.!!!
            // NB, set the mask flag if we are constructing a client frame
            byte maskBitSetAsByte = 0x00;

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
                WSHelper.WriteUShort((ushort)msg.Length, memoryStream);
            }
            else
            {
                throw new NotSupportedException("Message length too big");
            }

            memoryStream.Write(msg, 0, msg.Length);

            return buffer;
        }


    }
}
