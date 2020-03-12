﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace StockExchangeNotificationService
{

    internal class WSMessageProcessor : IWSMessageProcessor
    {
        private class QueueItem
        {
            public QueueItem(string id, string message)
            {
                Id = id;
                Message = message;
            }
            public string Id { get; }
            public string Message { get; }
        }
        private static readonly ConcurrentQueue<QueueItem> _queue = new ConcurrentQueue<QueueItem>();

        private readonly IWsRepository _repository;
        private readonly ManualResetEventSlim _event;
        private readonly CancellationTokenSource _tokenSource;

        public WSMessageProcessor(IWsRepository repository)
        {
            _repository = repository;
            _event = new ManualResetEventSlim();
            _tokenSource = new CancellationTokenSource();

            ProcessWork();
        }
        public void ProocessMessage(string excludeId, string message)
        {
            _queue.Enqueue(new QueueItem(excludeId, message));
            _event.Set();
        }
        public void Stop()
        {
            _tokenSource.Cancel();
        }
        private void ProcessWork()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        _event.Wait(TimeSpan.FromMinutes(5), _tokenSource.Token);

                        _tokenSource.Token.ThrowIfCancellationRequested();

                        while (_queue.Count > 0)
                        {
                            if (_queue.TryDequeue(out QueueItem qi))
                            {
                                foreach (var ws in _repository.GetSessions(qi.Id))
                                {
                                    try
                                    {
                                        ws.SendMessage(qi.Message);
                                    }
                                    catch() // TODO:
                                    {

                                    }

                                }
                            }
                        }
                        
                        _event.Reset();
                    }
                    catch(OperationCanceledException)
                    {
                        return;
                    }
                    catch(Exception x)
                    {
                        Console.WriteLine($"{x.Message} => PROCESSOR ERROR");
                        return;
                    }

                }
                
            });
        }
    }
}
