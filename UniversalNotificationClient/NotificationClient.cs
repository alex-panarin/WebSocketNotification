using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UniversalNotificationClient.DataExchange;
using UniversalNotificationClient.Factory;

namespace UniversalNotificationClient
{
    public sealed class NotificationClient : INotificationClient
    {
        private readonly TcpClient _client;
        private readonly CancellationTokenSource _tokenSource;

        public event MessageRecievedEventHandler<string> EventMessage;
        public event MessageRecievedEventHandler<Exception> Error;

        private Stream Stream { get; set; }
        
        public NotificationClient()
        {
            _client = new TcpClient();
            _tokenSource = new CancellationTokenSource();
        }

        public void CloseConnection()
        {
            try
            {
                CloseConnectionAsync();
            }
            catch (Exception x)
            {
                OnSendError(x);
            }
            finally
            {
                _tokenSource.Dispose();
                _client.Dispose();
            }
            
        }
        public void OpenConnection(Uri url)
        {
            try
            {
                ConnectAsync(url);
            }
            catch(Exception x)
            {
                OnSendError(x);
            }
        }
        public void SendMessage(string message)
        {
            try
            {
                SendMessageAsync(message);
            }
            catch (Exception x)
            {
                OnSendError(x);
            }

        }

        private async void SendMessageAsync(string message)
        {
            if (!_client.Connected) return;

            await Stream.Request(WebSocketOption.TextFrame, message);
        }
        private async void ConnectAsync(Uri url)
        {
            if (!url.IsAbsoluteUri) throw new Exception("Connection string is not valid");

            await _client.ConnectAsync(url.Host, url.Port);
            
            Stream = _client.GetStream();

            RecieveMessageLoop();

            await Stream.Request(WebSocketOption.Handshake, url);
        }
        private async void CloseConnectionAsync()
        {
            _tokenSource.Cancel();
            
            if (!_client.Connected) return;

            await Stream.Request(WebSocketOption.ConnectionClose, string.Empty);
        }
        private void RecieveMessageLoop()
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
               while (!_tokenSource.IsCancellationRequested && _client.Connected)
               {
                    try
                    {
                        if (_client.Available < 3) continue;

                        NotificationResponse response = await Stream.Response(_tokenSource.Token);

                        _tokenSource.Token.ThrowIfCancellationRequested();

                        switch (response.Option)
                        {
                            case WebSocketOption.TextFrame:
                            case WebSocketOption.BinaryFrame:
                                OnSendAction(Notifications.Notify, response.Payload);
                                break;
                            case WebSocketOption.ConnectionClose:
                                _tokenSource.Cancel();
                                break;
                            case WebSocketOption.Handshake:
                                OnSendAction(Notifications.Connect, string.Empty);
                                break;
                            default:
                                continue;
                        }

                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception x)
                    {
                        OnSendError(x);
                    }
                }

                OnSendAction(Notifications.Disconnect, string.Empty);
            });
        }
        private void OnSendAction(Notifications option, string payload)
        {
            EventMessage?.DynamicInvoke(this, new NotificationResult<string> { Payload = payload, Option = option});
        } 
        private void OnSendError(Exception payload)
        {
            Error?.DynamicInvoke(this, new NotificationResult<Exception> { Payload = payload, Option = Notifications.Error });
        }
    }
}
