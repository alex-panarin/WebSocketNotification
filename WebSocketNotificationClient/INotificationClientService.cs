using System;

namespace WebSocketNotificationClient
{  
    public delegate void MessageRecievedEventHandler(INotificationClientService sender, NotificationMessage message); 
    public interface INotificationClientService : IDisposable
    {
        void CloseConnection();
        void OpenConnection(Uri url);
        void SendMessage(string message);
        event MessageRecievedEventHandler EventMessage;
    }
}