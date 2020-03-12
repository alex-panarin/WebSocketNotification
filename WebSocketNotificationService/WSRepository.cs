using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace StockExchangeNotificationService
{

    internal class WSRepository : IWsRepository
    {
        private static readonly ConcurrentDictionary<string, WsSession> _repository = new ConcurrentDictionary<string, WsSession>();
        
        public IEnumerable<WsSession> GetSessions(string excludeId)
        {
            foreach (var item in _repository.Where(k => k.Key != excludeId))
            {
                yield return item.Value;
            }
        }
        public void Register(WsSession client)
        {
            _repository.TryAdd(client.Id, client);
        }
        public void Remove(string key = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                _repository.Clear();
            }
            else
            {
                _repository.TryRemove(key, out var s );
            }
        }
    }

}
