using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeTrackerExample.Configuration;
using IntegrationService.Client;
using IntegrationService.Contracts.v3;
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
            _timer = new Timer(HeartbitRoutine, null, Timeout.Infinite, Timeout.Infinite);
        }
        
        public event EventHandler<FullRebuildRequiredEventArgs> OnFullRebuildRequired;

        public void Start()
        {
            if (_configurations.Any())
            {
                _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(10));
            }
        }

        private void HeartbitRoutine(object state)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
                {
                    return;
                }

                WriteWithColor("Heartbit", ConsoleColor.Green);

                _tryCount++;

                var request = new SyncMetadataRequest()
                {
                    Items = ConvertConfigurationToRequestItems(_configurations)
                };

                Sync(request);
            }
            catch (Exception e)
            {
                WriteWithColor(e.ToString(), ConsoleColor.Red);
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private static void WriteWithColor(string message, ConsoleColor color)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = prevColor;
        }

        private void Sync(SyncMetadataRequest request)
        {
            MetadataSyncResult result;
            if (!TrySyncMetadata(request, out result))
            {
                Console.WriteLine($"Sync failed with unexpected error");
                return;
            }

            if (!result.NoFailedSyncs)
            {
                Console.WriteLine($"Sync failed: one or more sync items are failed");
                Console.WriteLine(result);
                return;
            }
            
            if (!result.FullRebuildRequired && !result.FullRebuildInProgress)
            {
                DisableHeartbitWithSuccess();
            }
            else
            {
                foreach (var frb in result.FullRebuildInProgressItems)
                {
                    WriteWithColor($"Full rebuild in progress for {frb.Request.SourceTypeFullName}", ConsoleColor.Yellow);
                }

                foreach (var frb in result.FullRebuildRequiredItems)
                {
                    Console.WriteLine($"Request full rebuild for {frb.Request.SourceTypeFullName}");
                    OnFullRebuildRequired?.Invoke(this, new FullRebuildRequiredEventArgs(frb.Request.SourceTypeFullName));
                }
            }
        }

        private bool TrySyncMetadata(SyncMetadataRequest request, out MetadataSyncResult result)
        {
            try
            {
                var response = _client.SyncMetadata(request);
                result = new MetadataSyncResult(request, response, _tryCount);
                return true;
            }
            catch (Exception e)
            {
                WriteWithColor(e.Message, ConsoleColor.Red);
                result = null;
                return false;
            }
        }
        
        private void DisableHeartbitWithSuccess()
        {
            Console.WriteLine("Heartbit disabled: sync succeeded");

            lock (_lock)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
        
        private SyncMetadataRequestItem[] ConvertConfigurationToRequestItems(EntityConfig[] confiurations)
        {
            return _configurations.Select(e => new SyncMetadataRequestItem()
            {
                Schema = e.Entity.MappingSchema,
                SourceTypeFullName = e.Entity.SourceType.FullName,
                EntityName = e.FullName,
                QueueName = e.DestinationQueue.Name
            }).ToArray();
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
