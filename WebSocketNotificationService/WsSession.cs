using StockExchangeDataModel;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static StockExchangeNotificationService.WSEncoder;

namespace StockExchangeNotificationService
{
    internal class WsSession : IDisposable
    {
        private readonly IWsDecoder _decoder;
        private readonly IWSEncoder _encoder;
        private bool _isDisposed;

        private TcpClient Client { get; }
        private Stream ClientStream { get; }

        public event EventHandler<WsSession> HandshakeCompleted;
        public event EventHandler<WsSession> Disconnected;
        public event EventHandler<Exception> Error;

        public event EventHandler<string> AnyMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> BinaryMessageReceived;
        
        public string Id { get; }

        internal WsSession(TcpClient client, IWsDecoder decoder, IWSEncoder encoder)
        {
            Client = client;
            ClientStream = client.GetStream();
            _decoder = decoder;
            _encoder = encoder;

            Id = Guid.NewGuid().ToString();
        }
        internal void StartSession()
        {
            // enter to an infinite cycle to be able to handle every change in stream
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    DoHandshake();
                    OnSendEvent(HandshakeCompleted, this);
                }
                catch(Exception x)
                {
                    OnSendEvent(Error, new Exception($"{x.Message} => Handshake Failed."));
                    OnSendEvent(Disconnected, this);

                    return;
                }

                StartMessageLoop();
            });
            
        }
        private void StartMessageLoop()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    MessageLoop();
                }
                catch(Exception x)
                {
                    OnSendEvent(Error, new Exception($"{x.Message} => MessageLoop Error"));
                }
                finally
                {
                    OnSendEvent(Disconnected, this);
                }
            });
            
        }
        private void DoHandshake()
        {
            while (Client.Available == 0 && Client.Connected) ;
            
            if (!Client.Connected)
            {
                throw new Exception("Client is not connected");
            }

            byte[] bytes = new byte[Client.Available];

            ClientStream.Read(bytes, 0, Client.Available);

            string s = Encoding.UTF8.GetString(bytes);

            if (!Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                throw new Exception("Handshake protocol error");
            }

            byte[] response = _decoder.DecodeHandshake(s);

            ClientStream.Write(response, 0, response.Length);
        }
        private void MessageLoop()
        {
            while (Client.Connected)
            {
                if (Client.Available < 3) continue;

                byte[] bytes = new byte[Client.Available];

                ClientStream.Read(bytes, 0, Client.Available);

                WsDecoderResult result = _decoder.DecodePayload(bytes);

                try
                {
                    switch (result.Option)
                    {
                        case WebSocketOption.ConnectionClose:
                            Close();
                            break;
                        case WebSocketOption.TextFrame:
                            OnSendEvent(TextMessageReceived, result.Payload);
                            break;
                        case WebSocketOption.BinaryFrame:
                            OnSendEvent(BinaryMessageReceived, result.Payload);
                            throw new NotSupportedException("Binary frame is not supported");
                        case WebSocketOption.Ping:
                            OnSendEvent(AnyMessageReceived, "PING");
                            break;
                        case WebSocketOption.Pong:
                            OnSendEvent(AnyMessageReceived, "PONG");
                            break;
                    }

                }
                catch (Exception x)
                {
                    Console.WriteLine($"WS:{Id} => ERROR:{x.Message}");
                    throw x;
                }
            }
        }
        private void Write(WebSocketMessageTypeEx type, string message)
        {
            if (!Client.Connected) return;

            byte[] pl = _encoder.EncodeMessage(type, message);

            if (!ClientStream.CanWrite)
            {
                throw new IOException("STREAM ERROR");
            }
            
            ClientStream.Write(pl, 0, pl.Length);
            

        }
        private async void OnSendEvent(Delegate sender, object value)
        {
            await Task.Run( () =>  sender?.DynamicInvoke(this, value)).ConfigureAwait(true);
        }
        public void SendMessage(string message)
        {
            Write(WebSocketMessageTypeEx.Text, message);
        }
        public void SendPong()
        {
            Write(WebSocketMessageTypeEx.Pong, "");
        }
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                Close();
                
                ClientStream?.Dispose();
                Client?.Dispose();
            }
        }
        public void Close()
        {
            if (!Client.Connected) return;
            try
            {
                if (!_isDisposed)
                {
                    Write(WebSocketMessageTypeEx.Close, "Closed");
                }

                ClientStream.Close();
                Client.Close();
            }
            catch(Exception x)
            {
                Console.WriteLine($"{x.Message}, CLOSE ERROR");
            }
        }
    }
}
