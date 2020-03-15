using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniversalNotificationClient.DataExchange;
using UniversalNotificationClient.Validation;
using UniversalNotificationClient.WebSocketCoders;

namespace UniversalNotificationClient.Factory
{
    internal static class NotificationFactory 
    {
        public static async Task<NotificationResponse> Response(this Stream stream, CancellationToken token = default)
        {
            ArraySegment<byte> bytes = new ArraySegment<byte>(new byte[4096]);
            int count = await stream.ReadAsync(bytes, token);

            byte[] array = bytes.AsSpan(0, count).ToArray();

            NotificationResponse response = null;
            
            WebSocketOption option = ResponseValidator.ValidateOption(array);

            switch (option)
            {
                case WebSocketOption.ContinuationFrame:
                    break;
                case WebSocketOption.TextFrame:
                case WebSocketOption.BinaryFrame:
                    response = new PayloadNotificationResponse() { Bytes = array };
                    break;
                case WebSocketOption.ConnectionClose:
                    response = new CloseNotificationResponse() { Bytes = array };
                    break;
                case WebSocketOption.Ping:
                    break;
                case WebSocketOption.Pong:
                    response = new PongNotificationResponse() { Bytes = array };
                    break;
                case WebSocketOption.Handshake:
                    response = new HandshakeNotificationResponse() { Bytes = array };
                    break;
                
            }
            
            if (response == null) throw new NotSupportedException($"Option: {option} is Not supported");

            //ResponseValidator.ValidateResponse(response, request);

            return response.Decode();
        }

        private class HandshakeNotificationResponse : NotificationResponse
        {
            public HandshakeNotificationResponse()
            {
                Option = WebSocketOption.Handshake;
            }
        }
        private class PayloadNotificationResponse : NotificationResponse
        {
            public PayloadNotificationResponse()
            {
                Option = WebSocketOption.TextFrame;
            }
        }
        private class PongNotificationResponse : NotificationResponse
        {
            public PongNotificationResponse()
            {
                Option = WebSocketOption.Pong;
            }
        }
        private class CloseNotificationResponse : NotificationResponse
        {
            public CloseNotificationResponse()
            {
                Option = WebSocketOption.ConnectionClose;
            }
        }

        public static async Task<NotificationRequest> Request<TRequest>(this Stream stream, WebSocketOption option, TRequest message) 
        {
            NotificationRequest request = null;

            switch (option)
            {
                case WebSocketOption.ContinuationFrame:
                    break;
                case WebSocketOption.TextFrame:
                    request = new PayloadNotificationRequest(message?.ToString()) { Stream = stream };
                    break;
                case WebSocketOption.BinaryFrame:
                    break;
                case WebSocketOption.ConnectionClose:
                    request = new CloseNotificationRequest { Stream = stream };
                    break;
                case WebSocketOption.Ping:
                    request = new PingNotificationRequest { Stream = stream };
                    break;
                case WebSocketOption.Pong:
                    
                case WebSocketOption.Handshake:
                    request = new HandshakeNotificationRequest(url: message as Uri) { Stream = stream };
                    break;
            }

            if (request == null) throw new NotSupportedException($"Option: {option} is Not supported");

            return await request.Encode().WriteAsync();
        } 

        private class HandshakeNotificationRequest : NotificationRequest
        {
            public const string SecKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            public HandshakeNotificationRequest(Uri url)
            {
                Option = WebSocketOption.Handshake;

                string key = Guid.NewGuid().ToString() + SecKey;
                string shaBase64Key = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(key)));
                Payload =
                    $"GET {url.AbsolutePath} HTTP/1.1\r\n" +
                    $"Host: {url.Authority}\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Version: 13\r\n" +
                    $"Sec-WebSocket-Key: {shaBase64Key}\r\n\r\n";
            }
        }
        private class PingNotificationRequest : NotificationRequest
        {
            public PingNotificationRequest()
            {
                Option = WebSocketOption.Ping;
                Payload = "Ping";
            }
        }
        private class CloseNotificationRequest : NotificationRequest
        {
            public CloseNotificationRequest()
            {
                Option = WebSocketOption.ConnectionClose;
                Payload = "Close";
            }
        }
        private class PayloadNotificationRequest : NotificationRequest
        {
            public PayloadNotificationRequest(string payload)
            {
                Option = WebSocketOption.TextFrame;
                Payload = payload;
            }
        }
    }
   
}
