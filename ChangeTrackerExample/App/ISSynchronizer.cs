using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeTrackerExample.Configuration;
using IntegrationService.Client;
using IntegrationService.Contracts.v1;
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
        }

        private void SyncMetadataInternal()
        {
            var request = FormatRequest();

            var response = _client.SyncMetadata(request);

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
                    Name = e.DestinationExchange.Name,
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
