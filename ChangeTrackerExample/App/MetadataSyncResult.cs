using IntegrationService.Contracts.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.App
{
    internal class MetadataSyncResult
    {
        public MetadataSyncResult(SyncMetadataRequest request, SyncMetadataResponse response, int tryCount)
        {
            Request = request;
            Response = response;

            JoinedItems = 
                (
                    from rq in request.Items
                    join rs in response.Items on rq.EntityName equals rs.Name
                    select new MetadataSyncResultItem { Request = rq, Response = rs }
                ).ToArray();

            TryCount = tryCount;
            NoFailedSyncs = response.Items.All(e => e.Result == SyncMetadataResult.Success);

            FullRebuildRequiredItems = JoinedItems.Where(e => e.Response.FullRebuildRequired).ToArray();
            FullRebuildRequired = FullRebuildRequiredItems.Any();

            FullRebuildInProgressItems = JoinedItems.Where(e => e.Response.FullRebuildInProgress).ToArray();
            FullRebuildInProgress = FullRebuildInProgressItems.Any();
        }

        public int TryCount { get; }

        public bool NoFailedSyncs { get; }
        public bool FullRebuildRequired { get; }
        public bool FullRebuildInProgress { get; }

        public MetadataSyncResultItem[] JoinedItems { get; }
        public MetadataSyncResultItem[] FullRebuildRequiredItems { get; }
        public MetadataSyncResultItem[] FullRebuildInProgressItems { get; }

        public SyncMetadataRequest Request { get; }
        public SyncMetadataResponse Response { get; }

        public override string ToString()
        {
            return new
            {
                TryCount = TryCount,
                TotalEntities = JoinedItems.Length,
                Messages = JoinedItems.Select(e => e.Response.Message).ToArray(),
                SucceededEntities = JoinedItems.Count(e => e.Response.Result == SyncMetadataResult.Success),
                FailedByBusinessReasons = JoinedItems.Count(e => e.Response.Result == SyncMetadataResult.BusinessConstraintViolation),
                FailedByTemporaryReasons = JoinedItems.Count(e => e.Response.Result == SyncMetadataResult.TemporaryError),
                FailedByUnexpectedReasons = JoinedItems.Count(e => e.Response.Result == SyncMetadataResult.UnhandledError),
            }.ToString();
        }
    }

    internal class MetadataSyncResultItem
    {
        public SyncMetadataRequestItem Request { get; set; }
        public SyncMetadataResponseItem Response { get; set; }
    }
}
