namespace UniversalNotificationClient
{
    public enum Notifications
    {
        Error = 1,
        Connect,
        Disconnect,
        Notify
    }
    public class NotificationResult<TPayload>
    {
        public Notifications Option { get; set; }
        public TPayload Payload { get; set; }
    }
}
