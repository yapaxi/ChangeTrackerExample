﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeTrackerExample.Configuration;
using IntegrationService.Client;
using IntegrationService.Contracts.v2;
using System.Threading;
using EasyNetQ;
using ChangeTrackerExample.App.Events;

namespace ChangeTrackerExample.App
{
    public class ISSynchronizer : IDisposable
    {
        private readonly ISClient _client;
        private readonly TaskCompletionSource<object> _cmplSrc;
        private readonly EntityConfig[] _configurations;
        private readonly Timer _timer;
        private readonly object _lock;
        private int _tryCount;
        private int _inProgress;

        public ISSynchronizer(ISClient client, IEnumerable<EntityConfig> configurations)
        {
            _tryCount = 0;
            _lock = new object();
            _client = client;
            _configurations = configurations.ToArray();
            _cmplSrc = new TaskCompletionSource<object>();
            _timer = new Timer(SyncMetadata, null, Timeout.Infinite, Timeout.Infinite);
        }

        public event EventHandler<QueueFailedEventArgs> OnQueueFailed;
        public event EventHandler<ISSyncFailedEventArgs> OnSyncFailed;
        public event EventHandler<EventArgs> OnSyncSucceeded;
        
        public void Start()
        {
            if (_configurations.Any())
            {
                _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(10));
            }
        }

        private void SyncMetadata(object state)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
                {
                    return;
                }

                _tryCount++;
                SyncMetadataInternal();
            }
            catch (AggregateException e) when (e.InnerExceptions.All(q => q is TimeoutException))
            {
                OnQueueFailed?.Invoke(this, new QueueFailedEventArgs() { Exception = e.InnerException, TryCount = _tryCount });
            }
            catch (EasyNetQException e)
            {
                OnQueueFailed?.Invoke(this, new QueueFailedEventArgs() { Exception = e, TryCount = _tryCount });
            }
            catch (TimeoutException e)
            {
                OnQueueFailed?.Invoke(this, new QueueFailedEventArgs() { Exception = e, TryCount = _tryCount });
            }
            catch (Exception e)
            {
                OnQueueFailed?.Invoke(this, new QueueFailedEventArgs() { Exception = e, TryCount = _tryCount });
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private void SyncMetadataInternal()
        {
            var request = FormatRequest();

            Console.WriteLine("Sending sync request...");
            var response = _client.SyncMetadata(request);
            Console.WriteLine("Sync request sent");

            var result = (
                from rq in request.Items
                join rs in response.Items on rq.Name equals rs.Name
                select new { Request = rq, Response = rs }
            ).ToArray();

            var allSucceeded = result.All(e => e.Response.Result == SyncMetadataResult.Success);

            if (allSucceeded)
            {
                lock (_lock)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                OnSyncSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                OnSyncFailed?.Invoke(this, new ISSyncFailedEventArgs()
                {
                    TryCount = _tryCount,
                    TotalEntities = result.Length,
                    Messages = result.Select(e => e.Response.Message).ToArray(),
                    SucceededEntities = result.Count(e => e.Response.Result == SyncMetadataResult.Success),
                    FailedByBusinessReasons = result.Count(e => e.Response.Result == SyncMetadataResult.BusinessConstraintViolation),
                    FailedByTemporaryReasons = result.Count(e => e.Response.Result == SyncMetadataResult.TemporaryError),
                    FailedByUnexpectedReasons = result.Count(e => e.Response.Result == SyncMetadataResult.UnhandledError),
                });
            }
        }

        private SyncMetadataRequest FormatRequest()
        {
            return new SyncMetadataRequest()
            {
                Items = _configurations.Select(e => new SyncMetadataRequestItem()
                {
                    Schema = e.Entity.MappingSchema,
                    Name = e.FullName,
                    QueueName = e.DestinationQueue.Name
                }).ToArray()
            };
        }

        public void Dispose()
        {
            using (var ae = new AutoResetEvent(false))
            {
                lock (_lock)
                {
                    _timer.Dispose(ae);
                }

                ae.WaitOne();
            }
        }
    }
}
