namespace StockExchangeNotificationService
{
    internal class WsDecoderResult
    {
        public WSEncoder.WebSocketOption Option { get; set; }
        public string Payload { get; set; }
    }
    internal interface IWsDecoder
    {
        string DecodeMessage(byte[] bytes);
        byte[] DecodeHandshake(string message);
        WsDecoderResult DecodePayload(byte[] bytes);
    }
}