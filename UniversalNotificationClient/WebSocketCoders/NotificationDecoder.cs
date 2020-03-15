using System;
using System.Text;
using System.Text.RegularExpressions;
using UniversalNotificationClient.DataExchange;

namespace UniversalNotificationClient.WebSocketCoders
{

    internal static class NotificationDecoder 
    {
        public static NotificationResponse Decode(this NotificationResponse response)
        {
            if (response.Option == WebSocketOption.Handshake) return response;

            bool mask = (response.Bytes[1] & 0b10000000) != 0; // must be false, "All messages from the server to the client have this bit off"

            if (mask) throw new ArgumentException("MASK should not be 1");

            int msglen = response.Bytes[1]; 
            int offset = 2;

            if (msglen == 126)
            {
                msglen = BitConverter.ToUInt16(new byte[] { response.Bytes[3], response.Bytes[2] }, 0);
                offset = 4;
            }

            response.Payload =  Encoding.UTF8.GetString(new ArraySegment<byte>(response.Bytes).Slice(offset, msglen).ToArray());
            
            return response;
        }
       
        
       
    }
}
