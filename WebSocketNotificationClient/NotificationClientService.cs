using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketNotificationClient
{
    public sealed class NotificationClientService : INotificationClientService
    {
        private readonly CancellationTokenSource _tokenSource;
        public NotificationClientService()
        {
            Client = new ClientWebSocket();
            _tokenSource = new CancellationTokenSource();
        }
        public event MessageRecievedEventHandler EventMessage;
        public void CloseConnection()
        {
            CloseAsync();
        }
        public void Dispose()
        {
            IsDisposed = true;
        }
        public void OpenConnection(Uri url)
        {
            try
            {
                if (IsConnected) return;

                ConnectAsync(url);
            }
            catch(Exception x)
            {
                OnSendAction(NotificationFactory.Create(Notifications.Error, x.ToString()));
            }
        }
        public void SendMessage(string message)
        {
            SendAsync(message);
        }

        private ClientWebSocket Client { get; }
        private bool IsConnected
        {
            get
            {
                return
                    Client.State == WebSocketState.Open ||
                    Client.State == WebSocketState.Connecting;
            }
        }
        private bool IsDisposed { get; set; }
        private async void ConnectAsync(Uri url)
        {
            await Client.ConnectAsync(url, _tokenSource.Token);

            await RecieveMessageLoopAsync();
        }
        private async Task RecieveMessageLoopAsync()
        {
            OnSendAction(NotificationFactory.Create(Notifications.Connect, string.Empty));

            while (!_tokenSource.IsCancellationRequested && IsConnected)
            {
                var buffer = WebSocket.CreateClientBuffer(4096, 4096);
                try
                {
                    WebSocketReceiveResult result = await Client.ReceiveAsync(buffer, _tokenSource.Token);

                    if (result.CloseStatus.HasValue ||
                        (Client.State == WebSocketState.CloseReceived && result.MessageType == WebSocketMessageType.Close))
                    {
                        CloseAsync();
                    }

                    if (result == null || _tokenSource.IsCancellationRequested) break;

                    if (!result.EndOfMessage)
                    {
                        throw new NotSupportedException("Message too big");
                    }

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            OnSendAction(NotificationFactory.Create(buffer.Array));
                            break;
                        case WebSocketMessageType.Binary:
                            throw new NotSupportedException("Binary data is not supported");
                    }
                }
                catch(OperationCanceledException)
                {
                    break;
                }
                catch(Exception x)
                {
                    OnSendAction(NotificationFactory.Create(Notifications.Error, x.ToString()));
                }
            }

            OnSendAction(NotificationFactory.Create(Notifications.Disconnect, string.Empty));
        }
        private async void SendAsync(string message)
        {
            if (!IsConnected) return;

            await Client.SendAsync(new ArraySegment<byte>(NotificationFactory.Create(Notifications.Notify, message).ToJsonByteArray()),
                WebSocketMessageType.Text, true, _tokenSource.Token);
        }
        private async void CloseAsync()
        {
            if (!IsConnected) return;

            _tokenSource.Cancel();

            await Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
        }
        private void OnSendAction(NotificationPayload payload)
        {
            if (IsDisposed) return;

            EventMessage?.Invoke(this, payload);
        }
    }
}
