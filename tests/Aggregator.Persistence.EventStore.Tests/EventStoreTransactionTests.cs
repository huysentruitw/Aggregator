using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Aggregator.Persistence.EventStore.Tests
{
    public class EventStoreTransactionTests
    {
        private readonly Mock<IEventStoreConnection> _eventStoreConnectionMock = new Mock<IEventStoreConnection>();
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        [Fact]
        public void StoreEvents_ShouldCallStartTransactionAsync()
        {
            var eventStoreConnectionMock = new Mock<IEventStoreConnection>();
            var transaction = new EventStoreTransaction<string, object>(eventStoreConnectionMock.Object, _jsonSerializerSettings);
            // Will throw NRE because StartTransactionAsync returns null (as we can't easily mock an EventStoreTransaction)
            Func<Task> action = () => transaction.StoreEvents("some_id", 1, Enumerable.Empty<object>());
            action.Should().Throw<NullReferenceException>();
            eventStoreConnectionMock.Verify(x => x.StartTransactionAsync("some_id", 0, null), Times.Once);
        }

        [Fact]
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

            capturedEventData.Should().NotBeNull(because: "WriteAsync method of wrapped transaction not called");
            capturedEventData.Should().HaveCount(2);
            Encoding.UTF8.GetString(capturedEventData[0].Data).Should().Be("{\"$type\":\"Aggregator.Persistence.EventStore.Tests.EventStoreTransactionTests+EventA, Aggregator.Persistence.EventStore.Tests\"}");
            Encoding.UTF8.GetString(capturedEventData[1].Data).Should().Be("{\"$type\":\"Aggregator.Persistence.EventStore.Tests.EventStoreTransactionTests+EventB, Aggregator.Persistence.EventStore.Tests\"}");
        }

        [Fact]
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

            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });

            createTransactionMock.Verify(x => x(It.IsAny<IEventStoreConnection>(), It.IsAny<string>(), It.IsAny<long>()), Times.Once);

            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });

            createTransactionMock.Verify(x => x(It.IsAny<IEventStoreConnection>(), It.IsAny<string>(), It.IsAny<long>()), Times.Exactly(2));
        }

        [Fact]
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

            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });
            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });
            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });

            wrappedTransactionMocks.Should().HaveCount(3);
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

        [Fact]
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

            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });
            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });
            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });

            wrappedTransactionMocks.Should().HaveCount(3);
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

        [Fact]
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

            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });
            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });
            await transaction.StoreEvents("some_id", 1, new object[] { new EventA(), new EventB() });

            wrappedTransactionMocks.Should().HaveCount(3);
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
