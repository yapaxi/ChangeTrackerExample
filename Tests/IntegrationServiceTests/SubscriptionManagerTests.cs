using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IntegrationService.Host.Subscriptions;
using IntegrationService.Host.Middleware;
using Moq;
using EasyNetQ;
using NLog;
using IntegrationService.Host.DI;
using IntegrationService.Contracts.v3;
using IntegrationServiceTests.FakeImpl;
using RabbitModel;
using System.Collections.Generic;
using Common;
using IntegrationService.Host.DAL;
using Common.Runtime;

namespace IntegrationServiceTests
{
    [TestClass]
    public class SubscriptionManagerTests
    {
        private Mock<IRequestLifetimeHandler> _middleware;
        private RespondBusFake _isBus;
        private ConsumerBusFake _dataBus;
        private Mock<ILogger> _logger;
        private Mock<ILoggerFactory<ILogger>> _loggerFactory;
        private SubscriptionManager _sm;
        private readonly int _bulkBufferSize = 43;

        [TestInitialize]
        public void TestInit()
        {
            _middleware = new Mock<IRequestLifetimeHandler>();
            _isBus = new RespondBusFake();
            _dataBus = new ConsumerBusFake();
            _logger = new Mock<ILogger>();
            _loggerFactory = new Mock<ILoggerFactory<ILogger>>();

            _loggerFactory.Setup(e => e.CreateForType(It.IsAny<Type>())).Returns(() => _logger.Object);

            var busMock = new Mock<IBus>();
            busMock.Setup(e => e.Advanced).Returns(_dataBus);
            
            _sm = new SubscriptionManager(_middleware.Object, _isBus, busMock.Object, busMock.Object, _logger.Object, _loggerFactory.Object, _bulkBufferSize);
        }

        [TestMethod]
        public void MetadataSyncSubscription()
        {
            var request = new SyncMetadataRequest();
            var response = new SyncMetadataResponse();

            _middleware.Setup(e => e.Response<SyncMetadataRequest, SyncMetadataResponse>(request)).Returns(response);
            _sm.SubscribeOnMetadataSync();

            var result = _isBus.FakeSend<SyncMetadataRequest, SyncMetadataResponse>(request);

            Assert.AreEqual(response, result);
        }

        [TestMethod]
        public void SimpleMessagingSubscription()
        {
            var entityCount = 100500;
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var schema = new RuntimeMappingSchema(new MappingSchema(new MappingProperty[0], 1234, DateTime.UtcNow));
            var writeDst = new Mock<IWriteDestination>();

            _middleware.Setup(e => e.HandleDataMessage(
                It.Is<RawMessage>(q => q.Body == data && q.EntityCount == entityCount),
                It.Is<MessageInfo>(q => q.Schema == schema && q.Destination == writeDst.Object))).Verifiable();

            _sm.SubscribeOnDataFlow(DataMode.RowByRow, "azaza", "uzuzuz", schema, writeDst.Object);

            var props = new MessageProperties();
            props.Headers = new Dictionary<string, object>() { { ISMessageHeader.ENTITY_COUNT, entityCount } };
            _dataBus.Send(data, props, new MessageReceivedInfo());

            _middleware.Verify();
        }

        #region BULK

        [TestMethod]
        public void BulkMessagingSubscription_Count_Is_Less_Than_Max_Buffer_Size()
        {
            TestBulkSubscription(_bulkBufferSize - 1, null, Times.Never(), Times.Never());
        }

        [TestMethod]
        public void BulkMessagingSubscription_Count_Is_Less_Than_Max_Buffer_Size_But_Last_Sent()
        {
            TestBulkSubscription(_bulkBufferSize - 1, _bulkBufferSize - 1, Times.Once(), Times.Once(), sendLastMessage: true);
        }

        [TestMethod]
        public void BulkMessagingSubscription_Count_Equals_To_Max_Buffer_Size()
        {
            TestBulkSubscription(_bulkBufferSize, _bulkBufferSize, Times.Once(), Times.Once());
        }

        [TestMethod]
        public void BulkMessagingSubscription_Count_Is_Greater_Than_Max_Buffer_Size()
        {
            TestBulkSubscription(_bulkBufferSize + 1, _bulkBufferSize, Times.Once(), Times.Once());
        }

        [TestMethod]
        public void BulkMessagingSubscription_Count_Is_Double_Of_Max_Buffer_Size()
        {
            TestBulkSubscription(_bulkBufferSize * 2, _bulkBufferSize, Times.Exactly(2), Times.Exactly(2));
        }

        [TestMethod]
        public void BulkMessagingSubscription_Count_Is_LessThanDouble_Of_Max_Buffer_Size()
        {
            TestBulkSubscription(_bulkBufferSize * 2 - 1, _bulkBufferSize, Times.Once(), Times.Once());
        }

        [TestMethod]
        public void BulkMessagingSubscription_Count_Is_GreaterThanDouble_Of_Max_Buffer_Size()
        {
            TestBulkSubscription(_bulkBufferSize * 2 + 1, _bulkBufferSize, Times.Exactly(2), Times.Exactly(2));
        }

        private void TestBulkSubscription(int countToSend, int? expectedReceivedPerCall, Times exactFlushTimes, Times anyFlushTimes, bool sendLastMessage = false)
        {
            var entityCount = 100500;
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var schema = new Mock<IRuntimeMappingSchema>();
            var writeDst = new Mock<IWriteDestination>();

            _sm.SubscribeOnDataFlow(DataMode.Bulk, "azaza", "uzuzuz", schema.Object, writeDst.Object);

            for (int i = 0; i < countToSend; i++)
            {
                var props = new MessageProperties();

                props.Headers = new Dictionary<string, object>()
                {
                    { ISMessageHeader.ENTITY_COUNT, entityCount },
                    { ISMessageHeader.BATCH_IS_LAST, false },
                    { ISMessageHeader.BATCH_ORDINAL, -1 },
                };

                if (sendLastMessage && i == countToSend - 1)
                {
                    props.Headers[ISMessageHeader.BATCH_IS_LAST] = true;
                    _dataBus.Send(data, props, new MessageReceivedInfo());
                }
                else
                {
                    _dataBus.Send(data, props, new MessageReceivedInfo());
                }
            }

            if (expectedReceivedPerCall != null)
            {
                _middleware.Verify(e => e.HandleDataMessage(
                    It.Is<IReadOnlyCollection<RawMessage>>(q => q.Count == expectedReceivedPerCall.Value),
                    It.Is<MessageInfo>(q => q.Schema == schema.Object && q.Destination == writeDst.Object)), exactFlushTimes);
            }
            _middleware.Verify(e => e.HandleDataMessage(It.IsAny<IReadOnlyCollection<RawMessage>>(), It.IsAny<MessageInfo>()), anyFlushTimes);
            _middleware.Verify();
        }

        #endregion

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SubscribeUnsubscribe_DublicateSubscription()
        {
            _sm.SubscribeOnDataFlow(DataMode.Bulk, "a", "azaza", null, null);
            _sm.SubscribeOnDataFlow(DataMode.Bulk, "a", "azaza", null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SubscribeUnsubscribe_SameEntity_DifferentModes()
        {
            _sm.SubscribeOnDataFlow(DataMode.Bulk, "a", "azaza", null, null);
            _sm.SubscribeOnDataFlow(DataMode.RowByRow, "a", "azaza", null, null);
        }

        [TestMethod]
        public void SubscribeUnsubscribe_DifferentEntities_DifferentModes()
        {
            _sm.SubscribeOnDataFlow(DataMode.Bulk, "a", "azaza", null, null);
            _sm.SubscribeOnDataFlow(DataMode.RowByRow, "b", "azaza", null, null);
        }

        [TestMethod]
        public void SubscribeUnsubscribe_DifferentEntities_SameModes()
        {
            _sm.SubscribeOnDataFlow(DataMode.RowByRow, "a", "azaza", null, null);
            _sm.SubscribeOnDataFlow(DataMode.RowByRow, "b", "azaza", null, null);

            _sm.CloseAllEntitySubscriptions("a");
            _sm.CloseAllEntitySubscriptions("b");

            _sm.SubscribeOnDataFlow(DataMode.Bulk, "a", "azaza", null, null);
            _sm.SubscribeOnDataFlow(DataMode.Bulk, "b", "azaza", null, null);
        }

        [TestMethod]
        public void SubscribeUnsubscribe_SameEntity_DifferentModes_Unsubcribe()
        {
            var entity = "a";
            _sm.SubscribeOnDataFlow(DataMode.Bulk, entity, "azaza", null, null);
            _sm.CloseAllEntitySubscriptions(entity);
            _sm.SubscribeOnDataFlow(DataMode.RowByRow, entity, "azaza", null, null);
            _sm.CloseAllEntitySubscriptions(entity);
            _sm.SubscribeOnDataFlow(DataMode.Bulk, entity, "azaza", null, null);
            _sm.CloseAllEntitySubscriptions(entity);
            _sm.SubscribeOnDataFlow(DataMode.RowByRow, entity, "azaza", null, null);
        }
    }
}
