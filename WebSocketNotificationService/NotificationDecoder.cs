using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;

namespace WebSocketNotificationService
{
    internal class NotificationDecoderResult
    {
        public WebSocketOption Option { get; set; }
        public string Payload { get; set; }
        public byte[] Bytes
        {
            get { return Encoding.UTF8.GetBytes(Payload); }
        }
    }
    internal static class NotificationDecoder 
    {
        public static NotificationDecoderResult DecodeMessage(byte[] bytes)
        {
            string message = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(message, "^GET", RegexOptions.IgnoreCase))
            {
                return new NotificationDecoderResult
                {
                    Option = WebSocketOption.Handshake,
                    Payload = DecodeHandshake(message)
                };
            }
            else
            {
                return new NotificationDecoderResult
                {
                    Option = bytes[0].GetOption(),
                    Payload = DecodePayload(bytes)
                };
            }
        }
        private static string DecodePayload(byte[] bytes)
        {
            bool mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

            int msglen = bytes[1] - 128; // & 0111 1111 // Remove MASK_EXISTS bit 
            int offset = 2;

            if (msglen == 126)
            {
                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            }

            if (!mask) throw new ArgumentException("MASK is not valid");

            byte[] decoded = new byte[msglen];

            byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
            offset += 4;

            for (int i = 0; i < msglen; ++i)
            {
                decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);
            }

            return Encoding.UTF8.GetString(decoded);

        }
        private static string DecodeHandshake(string message)
        {
            //Console.WriteLine("=====Handshaking from client=====\n{0}", message);

            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            // 3. Compute SHA-1 and Base64 hash of the new value
            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            string swk = Regex.Match(message, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            return 
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n";
        }
       
    }
}
