using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalNotificationClient.DataExchange
{
    internal  class NotificationRequest
    {
        public Stream Stream { get; internal set; }
        public string Payload { get; set; }
        public WebSocketOption Option { get; set; }
        internal ArraySegment<byte> Bytes { get; set; }
        
        internal async Task<NotificationRequest> WriteAsync()
        {
            ArraySegment<byte> bytes = 
                Bytes == null || Bytes.Count != 0 
                ? Bytes 
                : Encoding.UTF8.GetBytes(Payload);

            await Stream.WriteAsync(bytes);

            return this;
        }
       
    }
   
}
