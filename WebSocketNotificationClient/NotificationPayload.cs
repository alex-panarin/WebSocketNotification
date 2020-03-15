using Newtonsoft.Json;
using System;
using System.Text;

namespace WebSocketNotificationClient
{  
    public enum Notifications
    {
        Error = 1,
        Connect,
        Disconnect,
        Notify
    }

    public sealed class NotificationPayload
    {
        public Notifications Reason { get; set; }
        public string Payload { get; set; }
        public override string ToString()
        {
            return $"{Reason}:{Payload}";
        }
        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
        public byte[] ToJsonByteArray()
        {
            return Encoding.UTF8.GetBytes(ToJsonString());
        }
    }
}
