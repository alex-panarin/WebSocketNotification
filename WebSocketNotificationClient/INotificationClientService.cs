using System;

namespace WebSocketNotificationClient
{  
    public delegate void MessageRecievedEventHandler(INotificationClientService sender, NotificationPayload payload); 
    public interface INotificationClientService : IDisposable
    {
        void CloseConnection();
        void OpenConnection(Uri url);
        void SendMessage(string message);
        event MessageRecievedEventHandler EventMessage;
    }
}