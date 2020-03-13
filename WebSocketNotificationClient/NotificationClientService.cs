
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
        private readonly INotificationFactory _factory;
        private CancellationTokenSource cancellation;

        public event MessageRecievedEventHandler EventMessage;

        public NotificationClientService()
        {
            _factory = new NotificationFactory();
        }

        public void OpenConnection(Uri url)
        {
            try
            {
                Task.Run(() => EnsureConnectAsync(url)).ConfigureAwait(true);
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
            Task.Run(() => SendPayloadAsync(_factory.Create(Notifications.Notify, message))).ConfigureAwait(true);
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
                    byte[] toSend = Encoding.UTF8.GetBytes(payload.ToJSON());
                    await _webSocket.SendAsync(new ArraySegment<byte>(toSend), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception x)
            {
                await CloseAsync();
                OnSendAction(NotificationMessageReasons.Error, x.ToString());
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

                    OnSendAction(NotificationMessageReasons.Connected);

                    try
                    {
                        await RecieveNotificationAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        OnSendAction(NotificationMessageReasons.Disconnected);

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
                    OnSendAction(NotificationMessageReasons.Error, x.ToString());
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
                        OnSendAction(NotificationMessageReasons.Message, payload.ToString());
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

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            return;
                        case WebSocketMessageType.Text:
                            payload = _factory.Create(Encoding.UTF8.GetString(buffer.Array));
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
        private void OnSendAction(NotificationMessageReasons reason, string message = null)
        {
            if (IsDisposed) return;

            EventMessage?.Invoke(this, new NotificationMessage(reason, message));
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
