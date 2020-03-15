using System;

namespace UniversalNotificationClient
{  

    public delegate void MessageRecievedEventHandler<TPayload>(INotificationClient sender, NotificationResult<TPayload> payload); 
    public interface INotificationClient
    {
        void CloseConnection();
        void OpenConnection(Uri url);
        void SendMessage(string message);
        event MessageRecievedEventHandler<string> EventMessage;
        event MessageRecievedEventHandler<Exception> Error;
    }
}