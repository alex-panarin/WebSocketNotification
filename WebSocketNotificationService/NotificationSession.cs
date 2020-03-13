using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace WebSocketNotificationService
{
    public class NotificationSession
    {
        private readonly CancellationTokenSource _tokenSource;

        private TcpClient Client { get; }
        private Stream Stream { get; }

        public event EventHandler<NotificationSession> HandshakeCompleted;
        public event EventHandler<NotificationSession> Disconnected;
        public event EventHandler<Exception> Error;

        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> BinaryMessageReceived;

        public string Id { get; }
        public void SendMessage(string message)
        {
            Write(WebSocketMessageType.Text, message);
        }
        public void CloseSession()
        {
            Write(WebSocketMessageType.Close, "Closed");
        }
        public void Start()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!_tokenSource.IsCancellationRequested && Client.Connected)
                {
                    try
                    {
                        _tokenSource.Token.ThrowIfCancellationRequested();

                        if (Client.Available < 3) continue;

                        byte[] bytes = new byte[Client.Available];

                        Stream.Read(bytes, 0, bytes.Length);

                        NotificationDecoderResult result = NotificationDecoder.DecodeMessage(bytes);

                        switch (result.Option)
                        {
                            case WebSocketOption.Handshake:
                                WriteAsync(result.Bytes);
                                OnSendEvent(HandshakeCompleted, this);
                                break;
                            case WebSocketOption.ConnectionClose:
                                Close();
                                break;
                            case WebSocketOption.TextFrame:
                                OnSendEvent(TextMessageReceived, result.Payload);
                                break;
                            case WebSocketOption.BinaryFrame:
                                OnSendEvent(BinaryMessageReceived, result.Payload);
                                break;
                            //throw new NotSupportedException("Binary frame is not supported");
                            case WebSocketOption.Ping:
                                Write(WebSocketMessageType.Pong, "");
                                break;
                            case WebSocketOption.Pong:
                                break;
                        }

                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception x)
                    {
                        OnSendEvent(Error, x);
                    }

                }
            });
        }
        internal NotificationSession(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();

            Id = Guid.NewGuid().ToString("N");

            _tokenSource = new CancellationTokenSource();
        }
        
        private void Write(WebSocketMessageType type, string message)
        {
            if (!Client.Connected) return;

            byte[] pl = NotificationEncoder.EncodeMessage(type, message);

            if (!Stream.CanWrite)
            {
                throw new IOException("STREAM ERROR");
            }

            WriteAsync(pl);
        }
        private async void WriteAsync(byte[] response)
        {
            await Stream.WriteAsync(response, 0, response.Length, _tokenSource.Token);
        }
        private void OnSendEvent(Delegate sender, object value)
        {
            sender?.DynamicInvoke(this, value);
        }
        private void Close()
        {
            try
            {
                CloseSession();
                
                Stream.Close();
                Client.Close();

                _tokenSource.Cancel();

                Stream.Dispose();
                Client.Dispose();
                _tokenSource.Dispose();
            }
            catch (Exception x)
            {
                Console.WriteLine($"{x.Message}, CLOSE ERROR");
            }
            finally
            {
                OnSendEvent(Disconnected, this);
            }
        }
    }
}
