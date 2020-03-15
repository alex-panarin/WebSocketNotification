
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketNotificationClient
{
    public sealed class NotificationClientService : IDisposable, INotificationClientService
    {
        private class ReconnectException : Exception
        {
            public ReconnectException()
                : base("RECONNECT")
            {

            }
        }

        private ClientWebSocket _webSocket;
        private CancellationTokenSource cancellation;

        public event MessageRecievedEventHandler EventMessage;

        public NotificationClientService()
        {
        }

        public void OpenConnection(Uri url)
        {
            try
            {
                Task.Run(() => EnsureConnectAsync(url)).ConfigureAwait(false);
            }
            catch(ReconnectException)
            {
                
            }
        }
        public void CloseConnection()
        {
            Task.Run(() => CloseAsync()).ConfigureAwait(true);
        }
        public void SendMessage(string message)
        {
            Task.Run(() => SendPayloadAsync(NotificationFactory.Create(Notifications.Notify, message)));
        }

        private bool IsConnected
        {
            get
            {
                return _webSocket != null
                    && (_webSocket.State == WebSocketState.Open
                    || _webSocket.State == WebSocketState.CloseSent
                    || _webSocket.State == WebSocketState.CloseReceived);

            }
        }
        private bool IsClosedByHand { get; set; }
        private bool IsDisposed { get; set; }

        private async Task SendPayloadAsync(NotificationPayload payload)
        {
            try
            {
                if (IsConnected)
                {
                    byte[] toSend = payload.ToJsonByteArray();
                    await _webSocket.SendAsync(new ArraySegment<byte>(toSend), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception x)
            {
                await CloseAsync();
                OnSendAction(NotificationFactory.Create(Notifications.Error, x.ToString()));
            }
        }
        private async Task EnsureConnectAsync(Uri url)
        {
            while (true)
            {
                try
                {
                    if (IsConnected) return;

                    EnsureCreateWebSocket();

                    IsClosedByHand = false;

                    await _webSocket.ConnectAsync(url, CancellationToken.None);

                    OnSendAction(NotificationFactory.Create(Notifications.Connect, string.Empty));

                    try
                    {
                        await RecieveNotificationAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        OnSendAction(NotificationFactory.Create(Notifications.Disconnect, string.Empty));

                        if (IsClosedByHand)
                        {
                            await _webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            return;
                        }
                    }
                    catch (Exception x)
                    {
                        if (x is WebSocketException we
                            && we.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                        {
                            throw new ReconnectException();
                            ///Debug.Write($"RECONNECT: {we.Message}");
                        }

                        throw x;
                    }
                }
                catch (ReconnectException)
                {
                    continue;
                }
                catch (Exception x)
                {
                    await CloseAsync();
                    OnSendAction(NotificationFactory.Create(Notifications.Error, x.ToString()));
                    return;
                }
            }
        }
        private async Task CloseAsync()
        {
            if (!IsConnected) return;

            IsClosedByHand = true;

            await _webSocket?.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

            cancellation.Cancel();
        }
        private async Task RecieveNotificationAsync()
        {
            try
            {
                NotificationPayload payload = null;

                while (IsConnected && !cancellation.IsCancellationRequested)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[Byte.MaxValue]);
                    if (payload != null)
                    {
                        //payload.Notification = Notifications.Notify;
                        OnSendAction(payload);
                        payload = null;
                    }

                    WebSocketReceiveResult result = null;

                    try
                    {
                        result = await _webSocket.ReceiveAsync(buffer, cancellation.Token);
                    }
                    catch (WebSocketException)
                    {
                        if (!IsClosedByHand) throw;
                    }

                    cancellation.Token.ThrowIfCancellationRequested();

                    if (result == null) return;

                    if (!result.EndOfMessage)
                    {
                        throw new NotSupportedException("Message too big");
                    }
                    
                    if( result.CloseStatus.HasValue && 
                        (  result.CloseStatus == WebSocketCloseStatus.NormalClosure 
                        || result.CloseStatus == WebSocketCloseStatus.Empty))
                    {
                        await CloseAsync();
                        return;
                    }

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            return;
                        case WebSocketMessageType.Text:
                            payload = NotificationFactory.Create(buffer.Array);
                            break;
                        case WebSocketMessageType.Binary:
                            throw new NotSupportedException("Binary data is not supported");
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void EnsureCreateWebSocket()
        {
            if (_webSocket != null)
            {
                _webSocket.Dispose();
            }

            if (cancellation != null)
            {
                cancellation.Dispose();
            }

            cancellation = new CancellationTokenSource();

            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;

        }
        private void OnSendAction(NotificationPayload payload)
        {
            if (IsDisposed) return;

            EventMessage?.Invoke(this, payload);
        }
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            //CloseConnection();

            if (cancellation != null)
            {
                cancellation.Dispose();
            }

            if (_webSocket != null)
            {
                _webSocket.Dispose();
            }

            
        }
    }
}
