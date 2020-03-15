using System;

namespace UniversalNotificationClient.DataExchange
{
    public enum WebSocketOption : byte
    {
        ContinuationFrame = 0,
        TextFrame = 1,
        BinaryFrame = 2,
        ConnectionClose = 8,
        Ping = 9,
        Pong = 10,
        Handshake = 21
    }

    public enum WebSocketMessageType : byte
    {
        Text = 0,
        Binary = 1,
        Close = 2,
        Ping = 3,
        Pong = 4

    }

    internal static class Extentions
    {
        public static WebSocketOption GetOption(this byte opcode)
        {
            return Enum.Parse<WebSocketOption>(((byte)(opcode & 0b00001111)).ToString());
        }
        public static byte GetOption(this WebSocketMessageType type)
        {
            switch (type)
            {
                case WebSocketMessageType.Text:
                    return (byte)WebSocketOption.TextFrame;
                case WebSocketMessageType.Binary:
                    return (byte)WebSocketOption.TextFrame;
                case WebSocketMessageType.Close:
                    return (byte)WebSocketOption.ConnectionClose;
                case WebSocketMessageType.Ping:
                    return (byte)WebSocketOption.Ping;
                case WebSocketMessageType.Pong:
                    return (byte)WebSocketOption.Pong;
                default:
                    break;
            }

            return 255;
        }
    }
}
