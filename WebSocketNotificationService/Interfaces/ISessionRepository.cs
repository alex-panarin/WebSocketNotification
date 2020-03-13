using System.Collections.Generic;

namespace WebSocketNotificationService
{
    public interface ISessionRepository
    {
        IEnumerable<NotificationSession> GetSessions();
        void Register(NotificationSession client);
        void Remove(NotificationSession client = null);
    }
}