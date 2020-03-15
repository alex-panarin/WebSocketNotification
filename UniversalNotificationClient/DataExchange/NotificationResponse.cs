using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalNotificationClient.DataExchange
{
    internal class NotificationResponse
    {
        public string Payload { get; internal set; }
        public WebSocketOption Option { get; internal set; }
        internal byte[] Bytes { get; set; }
        
        protected NotificationResponse()
        {
        }
        
    }

}
