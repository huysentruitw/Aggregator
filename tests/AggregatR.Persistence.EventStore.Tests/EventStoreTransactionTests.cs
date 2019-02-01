using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AggregatR.Persistence.EventStore.Tests
{
    [TestFixture]
    public class EventStoreTransactionTests
    {
        private readonly Mock<IEventStoreConnection> _eventStoreConnectionMock = new Mock<IEventStoreConnection>();
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        [Test]
        public void StoreEvents_ShouldCallStartTransactionAsync()
        {
            var _eventStoreConnectionMock = new Mock<IEventStoreConnection>();
            var transaction = new EventStoreTransaction<string, object>(_eventStoreConnectionMock.Object, _jsonSerializerSettings);
            // Will throw NRE because StartTransactionAsync returns null (as we can't easily mock an EventStoreTransaction)
            Assert.ThrowsAsync<NullReferenceException>(() => transaction.StoreEvents("some_id", 1, Enumerable.Empty<object>()));
            _eventStoreConnectionMock.Verify(x => x.StartTransactionAsync("some_id", 0, null), Times.Once);
        }

        [Test]
        public async Task StoreEvents_ShouldWriteEventDataToTransaction()
        {
            var events = new object[] { new EventA(), new EventB() };

            EventData[] capturedEventData = null;
            var wrappedTransactionMock = new Mock<IWrappedTransaction>();
            wrappedTransactionMock
                .Setup(x => x.WriteAsync(It.IsAny<EventData[]>()))
                .Callback<EventData[]>(data => capturedEventData = data)
                .Returns(Task.CompletedTask);

            var createTransactionMock = new Mock<Func<IEventStoreConnection, string, long, Task<IWrappedTransaction>>>();
            createTransactionMock
                .Setup(x => x(_eventStoreConnectionMock.Object, "some_id", 2))
                .ReturnsAsync(wrappedTransactionMock.Object);

            var transaction = new EventStoreTransaction<string, object>(
                connection: _eventStoreConnectionMock.Object,
                jsonSerializerSettings: new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto },
                createTransaction: createTransactionMock.Object);

            await transaction.StoreEvents("some_id", 3, events);

            createTransactionMock.Verify(x => x(It.IsAny<IEventStoreConnection>(), It.IsAny<string>(), It.IsAny<long>()), Times.Once);

            Assert.That(capturedEventData, Is.Not.Null, "WriteAsync method of wrapped transaction not called");
            Assert.That(capturedEventData, Has.Length.EqualTo(2));
            Assert.That(Encoding.UTF8.GetString(capturedEventData[0].Data), Is.EqualTo("{\"$type\":\"AggregatR.Persistence.EventStore.Tests.EventStoreTransactionTests+EventA, AggregatR.Persistence.EventStore.Tests\"}"));
            Assert.That(Encoding.UTF8.GetString(capturedEventData[1].Data), Is.EqualTo("{\"$type\":\"AggregatR.Persistence.EventStore.Tests.EventStoreTransactionTests+EventB, AggregatR.Persistence.EventStore.Tests\"}"));
        }

        [Test]
        public async Task StoreEvents_ShouldCreateAPendingTransactionForEachCall()
        {
            var createTransactionMock = new Mock<Func<IEventStoreConnection, string, long, Task<IWrappedTransaction>>>();
            createTransactionMock
                .Setup(x => x(_eventStoreConnectionMock.Object, "some_id", 0))
                .ReturnsAsync(new Mock<IWrappedTransaction>().Object);

            var transaction = new EventStoreTransaction<string, object>(
                connection: _eventStoreConnectionMock.Object,
                jsonSerializerSettings: _jsonSerializerSettings,
                createTransaction: createTransactionMock.Object);

            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });

            createTransactionMock.Verify(x => x(It.IsAny<IEventStoreConnection>(), It.IsAny<string>(), It.IsAny<long>()), Times.Once);

            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });

            createTransactionMock.Verify(x => x(It.IsAny<IEventStoreConnection>(), It.IsAny<string>(), It.IsAny<long>()), Times.Exactly(2));
        }

        [Test]
        public async Task Commit_ShouldCommitAndDisposeAllPendingTransactions()
        {
            var wrappedTransactionMocks = new List<Mock<IWrappedTransaction>>();

            var createTransactionMock = new Mock<Func<IEventStoreConnection, string, long, Task<IWrappedTransaction>>>();
            createTransactionMock
                .Setup(x => x(_eventStoreConnectionMock.Object, "some_id", 0))
                .ReturnsAsync(() =>
                {
                    var wrappedTransactionMock = new Mock<IWrappedTransaction>();
                    wrappedTransactionMocks.Add(wrappedTransactionMock);
                    return wrappedTransactionMock.Object;
                });

            var transaction = new EventStoreTransaction<string, object>(
                connection: _eventStoreConnectionMock.Object,
                jsonSerializerSettings: _jsonSerializerSettings,
                createTransaction: createTransactionMock.Object);

            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });
            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });
            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });

            Assert.That(wrappedTransactionMocks, Has.Count.EqualTo(3));
            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Never);

            await transaction.Commit();

            wrappedTransactionMocks[0].Verify(x => x.CommitAsync(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.CommitAsync(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.CommitAsync(), Times.Once);

            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Never);

            wrappedTransactionMocks[0].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Dispose(), Times.Once);

            // Dispose shouldn't have any side effect
            transaction.Dispose();

            wrappedTransactionMocks[0].Verify(x => x.CommitAsync(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.CommitAsync(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.CommitAsync(), Times.Once);

            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Never);

            wrappedTransactionMocks[0].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task Rollback_ShouldRollbackAndDisposeAllPendingTransactions()
        {
            var wrappedTransactionMocks = new List<Mock<IWrappedTransaction>>();

            var createTransactionMock = new Mock<Func<IEventStoreConnection, string, long, Task<IWrappedTransaction>>>();
            createTransactionMock
                .Setup(x => x(_eventStoreConnectionMock.Object, "some_id", 0))
                .ReturnsAsync(() =>
                {
                    var wrappedTransactionMock = new Mock<IWrappedTransaction>();
                    wrappedTransactionMocks.Add(wrappedTransactionMock);
                    return wrappedTransactionMock.Object;
                });

            var transaction = new EventStoreTransaction<string, object>(
                connection: _eventStoreConnectionMock.Object,
                jsonSerializerSettings: _jsonSerializerSettings,
                createTransaction: createTransactionMock.Object);

            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });
            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });
            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });

            Assert.That(wrappedTransactionMocks, Has.Count.EqualTo(3));
            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Never);

            await transaction.Rollback();

            wrappedTransactionMocks[0].Verify(x => x.CommitAsync(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.CommitAsync(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.CommitAsync(), Times.Never);

            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Once);

            wrappedTransactionMocks[0].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Dispose(), Times.Once);

            // Dispose shouldn't have any side effect
            transaction.Dispose();

            wrappedTransactionMocks[0].Verify(x => x.CommitAsync(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.CommitAsync(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.CommitAsync(), Times.Never);

            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Once);

            wrappedTransactionMocks[0].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task Dispose_ShouldRollbackAndDisposeAllPendingTransaction()
        {
            var wrappedTransactionMocks = new List<Mock<IWrappedTransaction>>();

            var createTransactionMock = new Mock<Func<IEventStoreConnection, string, long, Task<IWrappedTransaction>>>();
            createTransactionMock
                .Setup(x => x(_eventStoreConnectionMock.Object, "some_id", 0))
                .ReturnsAsync(() =>
                {
                    var wrappedTransactionMock = new Mock<IWrappedTransaction>();
                    wrappedTransactionMocks.Add(wrappedTransactionMock);
                    return wrappedTransactionMock.Object;
                });

            var transaction = new EventStoreTransaction<string, object>(
                connection: _eventStoreConnectionMock.Object,
                jsonSerializerSettings: _jsonSerializerSettings,
                createTransaction: createTransactionMock.Object);

            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });
            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });
            await transaction.StoreEvents("some_id", 1, new[] { new object(), new object() });

            Assert.That(wrappedTransactionMocks, Has.Count.EqualTo(3));
            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Never);

            transaction.Dispose();

            wrappedTransactionMocks[0].Verify(x => x.CommitAsync(), Times.Never);
            wrappedTransactionMocks[1].Verify(x => x.CommitAsync(), Times.Never);
            wrappedTransactionMocks[2].Verify(x => x.CommitAsync(), Times.Never);

            wrappedTransactionMocks[0].Verify(x => x.Rollback(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Rollback(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Rollback(), Times.Once);

            wrappedTransactionMocks[0].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[1].Verify(x => x.Dispose(), Times.Once);
            wrappedTransactionMocks[2].Verify(x => x.Dispose(), Times.Once);
        }

        private class EventA { }

        private class EventB { }
    }
}
