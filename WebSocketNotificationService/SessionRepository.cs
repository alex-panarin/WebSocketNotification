using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace WebSocketNotificationService
{

    internal class SessionRepository : ISessionRepository
    {
        private static readonly ConcurrentDictionary<string, NotificationSession> _repository = new ConcurrentDictionary<string, NotificationSession>();
        
        public IEnumerable<NotificationSession> GetSessions()
        {
            foreach (var item in _repository)
            {
                yield return item.Value;
            }
        }
        public void Register(NotificationSession client)
        {
            _repository.TryAdd(client.Id, client);
        }
        public void Remove(NotificationSession session = null)
        {
            if (session == null)
            {
                _repository.Clear();
            }
            else
            {
                _repository.TryRemove(session.Id, out _);
            } 
        }
    }

}
