using System;
using UniversalNotificationClient;

namespace NotificationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            NotificationClient service = new NotificationClient();
            service.EventMessage += Service_EventMessage;
            service.Error += Service_Error;

            service.OpenConnection(new Uri("ws://192.168.1.67:8081"));

            Console.ReadLine();

            service.CloseConnection();
        }

        private static void Service_Error(INotificationClient sender, NotificationResult<Exception> payload)
        {
            Console.WriteLine(payload.Payload);
        }

        private static void Service_EventMessage(INotificationClient sender, NotificationResult<string> payload)
        {
            switch (payload.Option)
            {
                case Notifications.Connect:
                    sender.SendMessage("Connect From client");
                    Console.WriteLine("Connected to sever");
                    break;
                case Notifications.Disconnect:
                    sender.CloseConnection();
                    break;
                case Notifications.Notify:
                    sender.SendMessage("From client " + payload.Payload);
                    Console.WriteLine(payload.Payload);
                    break;

            }
        }

    }
}
