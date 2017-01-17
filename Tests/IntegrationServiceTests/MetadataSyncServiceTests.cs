using Common;
using Common.Runtime;
using IntegrationService.Contracts.v3;
using IntegrationService.Host.DAL;
using IntegrationService.Host.Metadata;
using IntegrationService.Host.Services;
using IntegrationService.Host.Subscriptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;
using RabbitModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationServiceTests
{
    [TestClass]
    public class MetadataSyncServiceTests
    {
        private MetadataSyncService _service;
        private Mock<ISchemaPersistenceService> _persistence;
        private Mock<ISubscriptionManager> _subscriptionManager;
        
        [TestInitialize]
        public void TestInit()
        {
            _persistence = new Mock<ISchemaPersistenceService>();
            _subscriptionManager = new Mock<ISubscriptionManager>();
            var logger = new Mock<ILogger>();

            _service = new MetadataSyncService(_persistence.Object, _subscriptionManager.Object, logger.Object);
        }

        [TestMethod]
        public void SchemaMatched_NoSubscription()
        {
            TestMetadataSync(
                givenStatus: new SchemaStatus("a", fullRebuildRequired: false, isActive: true),
                givenBulkSubscriptionExists: false,
                givenRowByRowSubscriptionExists: false,
                expectedSubscriptionDataMode: DataMode.RowByRow,
                expectedFullRebuildRequired: false,
                expectedFullRebuildInprogress: false,
                expectedResult: SyncMetadataResult.Success);
        }

        [TestMethod]
        public void SchemaMatched_BulkSubscriptionExists()
        {
            TestMetadataSync(
                givenStatus: new SchemaStatus("a", fullRebuildRequired: false, isActive: true),
                givenBulkSubscriptionExists: true,
                givenRowByRowSubscriptionExists: false,
                expectedSubscriptionDataMode: null,
                expectedFullRebuildRequired: false,
                expectedFullRebuildInprogress: true,
                expectedResult: SyncMetadataResult.Success);
        }

        [TestMethod]
        public void SchemaMatched_RowByRowSubscriptionExists()
        {
            TestMetadataSync(
                givenStatus: new SchemaStatus("a", fullRebuildRequired: false, isActive: true),
                givenBulkSubscriptionExists: false,
                givenRowByRowSubscriptionExists: true,
                expectedSubscriptionDataMode: null,
                expectedFullRebuildRequired: false,
                expectedFullRebuildInprogress: false,
                expectedResult: SyncMetadataResult.Success);
        }

        [TestMethod]
        public void SchemaNotMatched_NoSubscription()
        {
            TestMetadataSync(
                givenStatus: new SchemaStatus("a", fullRebuildRequired: true, isActive: true),
                givenBulkSubscriptionExists: false,
                givenRowByRowSubscriptionExists: false,
                expectedSubscriptionDataMode: DataMode.Bulk,
                expectedFullRebuildRequired: true,
                expectedFullRebuildInprogress: true,
                expectedResult: SyncMetadataResult.Success);
        }

        [TestMethod]
        public void SchemaNotMatched_RowByRowSubscriptionExists()
        {
            TestMetadataSync(
                givenStatus: new SchemaStatus("a", fullRebuildRequired: true, isActive: true),
                givenBulkSubscriptionExists: false,
                givenRowByRowSubscriptionExists: true,
                expectedSubscriptionDataMode: DataMode.Bulk,
                expectedFullRebuildRequired: true,
                expectedFullRebuildInprogress: true,
                expectedResult: SyncMetadataResult.Success);
        }

        [TestMethod]
        public void SchemaNotMatched_BulkSubscriptionExists()
        {
            TestMetadataSync(
                givenStatus: new SchemaStatus("a", fullRebuildRequired: true, isActive: true),
                givenBulkSubscriptionExists: true,
                givenRowByRowSubscriptionExists: false,
                expectedSubscriptionDataMode: null,
                expectedFullRebuildRequired: false,
                expectedFullRebuildInprogress: true,
                expectedResult: SyncMetadataResult.Success);
        }

        private void TestMetadataSync(
            SchemaStatus givenStatus,
            bool givenBulkSubscriptionExists,
            bool givenRowByRowSubscriptionExists,
            DataMode? expectedSubscriptionDataMode = null,
            bool expectedFullRebuildRequired = false,
            bool expectedFullRebuildInprogress = false,
            SyncMetadataResult expectedResult = SyncMetadataResult.Invalid)
        {
            var item = new SyncMetadataRequestItem()
            {
                EntityName = givenStatus.EntityName,
                QueueName = "a.queue",
                SourceTypeFullName = "a",
                Schema = new MappingSchema(new MappingProperty[0], 100500, DateTime.UtcNow)
            };

            var table = new Mock<IWriteDestination>();

            _persistence.Setup(e => e.GetSchemaStatus(item.EntityName, item.QueueName, item.Schema)).Returns(givenStatus);
            _subscriptionManager.Setup(e => e.SubscriptionExists(item.EntityName, DataMode.Bulk)).Returns(givenBulkSubscriptionExists);
            _subscriptionManager.Setup(e => e.SubscriptionExists(item.EntityName, DataMode.RowByRow)).Returns(givenRowByRowSubscriptionExists);
            _persistence.Setup(e => e.UseSchema(item.EntityName, item.QueueName, item.Schema)).Returns(table.Object);

            var response = _service.Response(new SyncMetadataRequest()
            {
                Items = new List<SyncMetadataRequestItem>() { item }.ToArray()
            });

            _persistence.Verify();

            if (expectedSubscriptionDataMode != null)
            {
                _persistence.Verify(e => e.UseSchema(item.EntityName, item.QueueName, item.Schema), Times.Once);
                _subscriptionManager.Verify(e => e.SubscribeOnDataFlow(
                     expectedSubscriptionDataMode.Value,
                     item.EntityName, item.QueueName,
                     It.IsAny<IRuntimeMappingSchema>(),
                     It.IsAny<IWriteDestination>()), Times.Once);
            }
            else
            {
                _persistence.Verify(e => e.UseSchema(item.EntityName, item.QueueName, item.Schema), Times.Never);
                _subscriptionManager.Verify(e => e.SubscribeOnDataFlow(
                    It.IsAny<DataMode>(),
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IRuntimeMappingSchema>(),
                    It.IsAny<IWriteDestination>()), Times.Never);
            }

            Assert.AreEqual(response.Items.Length, 1);
            var responseItem = response.Items.Single();

            Assert.AreEqual(givenStatus.EntityName, responseItem.Name);
            Assert.AreEqual(expectedFullRebuildRequired, responseItem.FullRebuildRequired);
            Assert.AreEqual(expectedFullRebuildInprogress, responseItem.FullRebuildInProgress);
            Assert.AreEqual(expectedResult, responseItem.Result);
        }
    }
}
