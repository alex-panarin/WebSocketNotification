using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace StockExchangeNotificationService
{
    public class WsServer
    {
        private readonly IWsRepository _repository;
        private readonly IWSMessageProcessor _processor;
        private readonly CancellationTokenSource _tokenSource;
        private  TcpListener _listner;
        
        internal WsServer(IWsRepository repository, IWSMessageProcessor processor)
        {
            _repository = repository;
            _processor = processor;
            _tokenSource = new CancellationTokenSource();
        }
        public static void StartServer()
        {
            int port = 8081;

            WSRepository rep = new WSRepository();
            WsServer server =
                new WsServer(rep,
                new WSMessageProcessor(rep));
            try
            {
                server.Listen(port);
                Console.ReadKey();
            }
            catch (Exception x)
            {
                Console.WriteLine($"{x} => Unhandled Exception");
            }
            finally
            {
                server.Stop();
            }
            
        }
        public void Stop()
        {
            _processor.Stop();

            foreach (var item in _repository.GetSessions(string.Empty))
            {
                item.Dispose();
            }

            _tokenSource.Cancel();
            _listner.Stop();
        }
        private void Listen(int port)
        {
            _listner = new TcpListener(IPAddress.Any, port);
            _listner.Start();

            Console.WriteLine("Server has started on {0}, Waiting for a connection...", _listner.LocalEndpoint);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        WsSession ws =
                            new WsSession(_listner.AcceptTcpClient(), // Blocking operation
                            new WSDecoder(),
                            new WSEncoder());

                        _tokenSource.Token.ThrowIfCancellationRequested();

                        ws.Error += OnError;
                        ws.Disconnected += OnDisconnected;
                        ws.HandshakeCompleted += OnHandshakeCompleted;
                        ws.AnyMessageReceived += OnAnyMessageRecieved;
                        ws.TextMessageReceived += OnTextMessageRecieved;

                        ws.StartSession();
                    }
                    catch(OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine($"{x} => SERVER SESSION ERROR");
                        throw x;
                    }
                }
            });
        }
        private void OnHandshakeCompleted(object s, WsSession e)
        {
            Console.WriteLine($"{e.Id} => Handshake comlplete");
            _repository.Register(e);
        }
        private void OnDisconnected(object s, WsSession e)
        {
            Console.WriteLine($"{e.Id} => Disconnected");
            _repository.Remove(e.Id);

            e.HandshakeCompleted    -= OnHandshakeCompleted;
            e.Disconnected          -= OnDisconnected;
            e.Error                 -= OnError;
            e.AnyMessageReceived    -= OnAnyMessageRecieved;
            e.TextMessageReceived   -= OnTextMessageRecieved;
            
            e.Dispose();
        }
        private void OnTextMessageRecieved(object s, string e)
        {
            Console.WriteLine($"{e} => Message");

            _processor.ProocessMessage(((WsSession)s).Id, e); 
        }
        private void OnAnyMessageRecieved(object s, string e)
        {
            Console.WriteLine($"{e} => Ping Pong");

            if (s is WsSession wss && e == "PING")
            {
                wss.SendPong();
            }
        }
        private void OnError(object s, Exception e)
        {
            Console.WriteLine($"{e.Message} => ERROR");
        }
    }
}
