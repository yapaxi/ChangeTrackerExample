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

        [TestMethod]
        public void BulkMessagingSubscription()
        {
            var entityCount = 100500;
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var schema = new RuntimeMappingSchema(new MappingSchema(new MappingProperty[0], 1234, DateTime.UtcNow));
            var writeDst = new Mock<IWriteDestination>();

            _middleware.Setup(e => e.HandleDataMessage(
                It.Is<IReadOnlyCollection<RawMessage>>(q => q.Count == _bulkBufferSize),
                It.Is<MessageInfo>(q => q.Schema == schema && q.Destination == writeDst.Object))).Verifiable();

            _sm.SubscribeOnDataFlow(DataMode.Bulk, "azaza", "uzuzuz", schema, writeDst.Object);

            var props = new MessageProperties();
            props.Headers = new Dictionary<string, object>()
            {
                { ISMessageHeader.ENTITY_COUNT, entityCount },
                { ISMessageHeader.BATCH_IS_LAST, false },
                { ISMessageHeader.BATCH_ORDINAL, -1 },
            };

            for (int i = 0; i < _bulkBufferSize; i++)
            {
                _dataBus.Send(data, props, new MessageReceivedInfo());
            }

            _middleware.Verify();
        }
    }
}
