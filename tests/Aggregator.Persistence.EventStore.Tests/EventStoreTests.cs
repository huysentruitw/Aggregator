using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Aggregator.Persistence.EventStore.Tests
{
    public class EventStoreTests
    {
        private Mock<IEventStoreConnection> NewEventStoreConnectionMock => new Mock<IEventStoreConnection>();

        [Fact]
        public void Constructor_PassInvalidConnection_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => new EventStore((IEventStoreConnection)null);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("connection");
        }

        [Fact]
        public void Dispose_ShouldDisposeUnderlyingConnection()
        {
            // Arrange
            var eventStoreConnectionMock = NewEventStoreConnectionMock;
            var eventStore = new EventStore(eventStoreConnectionMock.Object);
            eventStoreConnectionMock.Verify(x => x.Dispose(), Times.Never);

            // Act
            eventStore.Dispose();

            // Assert
            eventStoreConnectionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task Contains_EventStreamForAggregateRootNotFound_ShouldReturnFalse()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var eventStoreConnectionMock = NewEventStoreConnectionMock;
            eventStoreConnectionMock
                .Setup(x => x.ReadEventAsync(identifier, 0, false, null))
                .ReturnsAsync(CreateEventReadResult(EventReadStatus.NoStream));
            var eventStore = new EventStore(eventStoreConnectionMock.Object);

            // Act
            var result = await eventStore.Contains(identifier);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Contains_EventStreamForAggregateRootFound_ShouldReturnTrue()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var eventStoreConnectionMock = NewEventStoreConnectionMock;
            eventStoreConnectionMock
                .Setup(x => x.ReadEventAsync(identifier, 0, false, null))
                .ReturnsAsync(CreateEventReadResult(EventReadStatus.Success));
            var eventStore = new EventStore(eventStoreConnectionMock.Object);

            // Act
            var result = await eventStore.Contains(identifier);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetEvents_PassingMinimumVersion_ShouldStartReadingEventsFromMinimumVersion()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var events = new object[] { new EventB(), new EventA() };
            var eventStoreConnectionMock = NewEventStoreConnectionMock;
            eventStoreConnectionMock
                .Setup(x => x.ReadStreamEventsForwardAsync(identifier, 4, It.IsAny<int>(), false, null))
                .ReturnsAsync(CreateStreamEventSlice(events, true));
            var eventStore = new EventStore(eventStoreConnectionMock.Object);

            // Act
            var result = await eventStore.GetEvents(identifier, 4);

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().BeOfType<EventB>();
            result[1].Should().BeOfType<EventA>();
        }

        [Fact]
        public void BeginTransaction_ShouldReturnAnEventStoreTransactionInstance()
        {
            // Arrange
            var eventStore = new EventStore(NewEventStoreConnectionMock.Object);

            // Act
            var transaction = eventStore.BeginTransaction(null);

            // Assert
            transaction.Should().NotBeNull();
            transaction.Should().BeOfType<EventStoreTransaction<string, object>>();
        }

        private static EventReadResult CreateEventReadResult(EventReadStatus status)
        {
            var result = (EventReadResult)FormatterServices.GetUninitializedObject(typeof(EventReadResult));
            typeof(EventReadResult)
                .GetField(nameof(EventReadResult.Status), BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(result, status);
            return result;
        }

        private static StreamEventsSlice CreateStreamEventSlice(object[] events, bool isEndOfStream)
        {
            var result = (StreamEventsSlice)FormatterServices.GetUninitializedObject(typeof(StreamEventsSlice));
            typeof(StreamEventsSlice)
                .GetField(nameof(StreamEventsSlice.Events), BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(result, CreateResolvedEvents(events));
            typeof(StreamEventsSlice)
                .GetField(nameof(StreamEventsSlice.IsEndOfStream), BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(result, isEndOfStream);
            return result;
        }

        private static ResolvedEvent[] CreateResolvedEvents(object[] events)
        {
            return events
                .Select(x =>
                {
                    byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x, typeof(object), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                    }));

                    var @event = (RecordedEvent)FormatterServices.GetUninitializedObject(typeof(RecordedEvent));
                    typeof(RecordedEvent)
                        .GetField(nameof(RecordedEvent.Data), BindingFlags.Instance | BindingFlags.Public)
                        ?.SetValue(@event, data);

                    var resolvedEvent = (object)FormatterServices.GetUninitializedObject(typeof(ResolvedEvent));
                    typeof(ResolvedEvent)
                        .GetField(nameof(ResolvedEvent.Event), BindingFlags.Instance | BindingFlags.Public)
                        ?.SetValue(resolvedEvent, @event);
                    return (ResolvedEvent)resolvedEvent;
                })
                .ToArray();
        }

        private class EventA
        {
        }

        private class EventB
        {
        }
    }
}
