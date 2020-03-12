using System.Collections.Generic;

namespace StockExchangeNotificationService
{
    internal interface IWsRepository
    {
        IEnumerable<WsSession> GetSessions(string excludeId);
        void Register(WsSession client);
        void Remove(string key = null);
    }
}