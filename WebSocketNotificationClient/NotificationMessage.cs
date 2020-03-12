namespace StockExchangeNotificationClient
{
    public enum NotificationMessageReasons
    {
        Error = 1,
        Connected,
        Disconnected,
        Message
    }
    public class NotificationMessage
    {
        public NotificationMessageReasons Reason { get; set; }
        public string Message{ get; set; }
        public NotificationMessage(NotificationMessageReasons reason, string message)
        {
            Reason = reason;
            Message = message;
        }
    }
}