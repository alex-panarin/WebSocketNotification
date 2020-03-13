using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketNotificationService
{
    public sealed class NotificatinService : BackgroundService
    {
        private readonly ILogger<NotificatinService> _logger;
        private readonly ISessionRepository _repository;
        private readonly IMessageProcessor _processor;
        private readonly TcpListener _listener;

        public NotificatinService(
            ILogger<NotificatinService> logger, 
            ISessionRepository repository,
            IMessageProcessor processor)
        {
            _logger = logger;
            _repository = repository;
            _processor = processor;

            int port = int.Parse(Program.Configuration.GetSection("ConnectionEndpoints")["Port"]);
            _listener = new TcpListener(IPAddress.Any, port);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener.Start();
            _logger.LogInformation("Server has been started on {0}, Waiting for a connection...", _listener.LocalEndpoint);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    SessionStart(new NotificationSession(await _listener.AcceptTcpClientAsync()));
                }
            }
            catch (Exception x)
            {
                _logger.LogError(x, "ExecuteAsync");
            }

        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var item in _repository.GetSessions())
            {
                item.CloseSession();
            }
            _repository.Remove();

            return base.StopAsync(cancellationToken);
        }

        private void SessionStart(NotificationSession session)
        {
            session.HandshakeCompleted += Session_HandshakeCompleted;
            session.TextMessageReceived += Session_TextMessageReceived;
            session.Disconnected += Session_Disconnected;
            session.Error += Session_Error;

            session.Start();
        }
        private void Session_Error(object sender, Exception e)
        {
            _logger.LogError(e, "Error in session {0}", ((NotificationSession)sender).Id);
        }
        private void Session_Disconnected(object sender, NotificationSession e)
        {
            _repository.Remove(e);

            e.HandshakeCompleted -= Session_HandshakeCompleted;
            e.TextMessageReceived -= Session_TextMessageReceived;
            e.Disconnected -= Session_Disconnected;
            e.Error -= Session_Error;

            _logger.LogDebug("Client was disconnected by Id: {0}", e.Id);
        }
        private void Session_TextMessageReceived(object sender, string e)
        {
            _processor.ProocessMessage(((NotificationSession)sender).Id, e);
        }
        private void Session_HandshakeCompleted(object sender, NotificationSession e)
        {
            _repository.Register(e);
            _logger.LogDebug("Client was connected by Id: {0}", e.Id);
        }
    }
}
