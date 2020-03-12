using static StockExchangeNotificationService.WSEncoder;

namespace StockExchangeNotificationService
{

    public interface IWSEncoder
    {
        byte[] EncodeMessage(WebSocketMessageTypeEx messageType, string message, bool isLastFrame = true);
    }
}