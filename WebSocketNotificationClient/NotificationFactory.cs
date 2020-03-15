using Newtonsoft.Json;
using System;
using System.Text;

namespace WebSocketNotificationClient
{
    internal static class NotificationFactory 
    {
        public static NotificationPayload Create(Notifications notifications, string payload)
        {
            return new NotificationPayload
            {
                Reason = notifications,
                Payload = payload,
            };
        }
        public static NotificationPayload Create(byte[] byteArray)
        {
            string payload = Encoding.UTF8.GetString(byteArray);

            if (!payload.Contains(":")) return null;

            if (payload.Contains("\"")) // JSON string expected
            {
                return JsonConvert.DeserializeObject<NotificationPayload>(payload);
            }

            return new NotificationPayload
            {
                Reason = Enum.Parse<Notifications>(payload.Split(':')[0]),
                Payload = payload.Split(':')[1]
            };
        }
    }
}
