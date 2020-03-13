using System.Collections.Generic;

namespace WebSocketNotificationService
{
    public interface IMessageProcessor
    {
        void ProocessMessage(string excludeId, string message);
        void Stop();
    }
}