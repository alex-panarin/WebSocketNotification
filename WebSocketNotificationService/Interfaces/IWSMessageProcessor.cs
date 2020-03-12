using System.Collections.Generic;

namespace StockExchangeNotificationService
{

    internal interface IWSMessageProcessor
    {
        void ProocessMessage(string excludeId, string message);
        void Stop();
    }
}