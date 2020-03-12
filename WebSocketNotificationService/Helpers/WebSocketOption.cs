using System;
using System.ComponentModel;
using System.Net.WebSockets;

namespace StockExchangeNotificationService
{
    public partial class WSEncoder
    {
        public enum WebSocketOption
        {
            ContinuationFrame = 0,
            TextFrame = 1,
            BinaryFrame = 2,
            ConnectionClose = 8,
            Ping = 9,
            Pong = 10

        }

        public enum WebSocketMessageTypeEx : byte
        {
            Text = 0,
            Binary = 1,
            Close = 2,
            Ping = 3,
            Pong = 4

        }
        public static WebSocketOption GetOption(byte opcode)
        {
            return Enum.Parse<WebSocketOption>(((byte)(opcode & 0b00001111)).ToString());
        }

        public static byte GetOption(WebSocketMessageTypeEx type)
        {
            switch(type)
            {
                case WebSocketMessageTypeEx.Text:
                    return (byte)WebSocketOption.TextFrame;
                case WebSocketMessageTypeEx.Binary:
                    return (byte)WebSocketOption.TextFrame;
                case WebSocketMessageTypeEx.Close:
                    return (byte)WebSocketOption.ConnectionClose;
                case WebSocketMessageTypeEx.Ping:
                    return (byte)WebSocketOption.Ping;
                case WebSocketMessageTypeEx.Pong:
                    return (byte)WebSocketOption.Pong;
            }

            return 255;
        }
    }
}
