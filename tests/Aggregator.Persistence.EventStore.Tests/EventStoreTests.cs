using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Aggregator.Persistence.EventStore.Tests
{
    [TestFixture]
    public class EventStoreTests
    {
        private readonly Mock<IEventStoreConnection> _eventStoreConnectionMock = new Mock<IEventStoreConnection>();

        [SetUp]
        public void SetUp()
        {
            _eventStoreConnectionMock.Reset();
        }

        [Test]
        public void Constructor_PassInvalidConnection_ShouldThrowException()
        {
            // Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new EventStore((IEventStoreConnection)null));
            Assert.That(ex.ParamName, Is.EqualTo("connection"));
        }

        [Test]
        public void Dispose_ShouldDisposeUnderlyingConnection()
        {
            // Arrange
            var eventStore = new EventStore(_eventStoreConnectionMock.Object);
            _eventStoreConnectionMock.Verify(x => x.Dispose(), Times.Never);

            // Act
            eventStore.Dispose();

            // Assert
            _eventStoreConnectionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task Contains_EventStreamForAggregateRootNotFound_ShouldReturnFalse()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            _eventStoreConnectionMock
                .Setup(x => x.ReadEventAsync(identifier, 0, false, null))
                .ReturnsAsync(CreateEventReadResult(EventReadStatus.NoStream));
            var eventStore = new EventStore(_eventStoreConnectionMock.Object);

            // Act
            var result = await eventStore.Contains(identifier);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task Contains_EventStreamForAggregateRootFound_ShouldReturnTrue()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            _eventStoreConnectionMock
                .Setup(x => x.ReadEventAsync(identifier, 0, false, null))
                .ReturnsAsync(CreateEventReadResult(EventReadStatus.Success));
            var eventStore = new EventStore(_eventStoreConnectionMock.Object);

            // Act
            var result = await eventStore.Contains(identifier);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task GetEvents_PassingMinimumVersion_ShouldStartReadingEventsFromMinimumVersion()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var events = new object[] { new EventB(), new EventA() };
            _eventStoreConnectionMock
                .Setup(x => x.ReadStreamEventsForwardAsync(identifier, 4, It.IsAny<int>(), false, null))
                .ReturnsAsync(CreateStreamEventSlice(events, true));
            var eventStore = new EventStore(_eventStoreConnectionMock.Object);

            // Act
            var result = await eventStore.GetEvents(identifier, 4);

            // Assert
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.InstanceOf<EventB>());
            Assert.That(result[1], Is.InstanceOf<EventA>());
        }

        [Test]
        public void BeginTransaction_ShouldReturnAnEventStoreTransactionInstance()
        {
            // Arrange
            var eventStore = new EventStore(_eventStoreConnectionMock.Object);

            // Act
            var transaction = eventStore.BeginTransaction(null);

            // Assert
            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction, Is.InstanceOf<EventStoreTransaction<string, object>>());
        }

        private static EventReadResult CreateEventReadResult(EventReadStatus status)
        {
            var result = (EventReadResult)FormatterServices.GetUninitializedObject(typeof(EventReadResult));
            typeof(EventReadResult)
                .GetField(nameof(EventReadResult.Status), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(result, status);
            return result;
        }

        private static StreamEventsSlice CreateStreamEventSlice(object[] events, bool isEndOfStream)
        {
            var result = (StreamEventsSlice)FormatterServices.GetUninitializedObject(typeof(StreamEventsSlice));
            typeof(StreamEventsSlice)
                .GetField(nameof(StreamEventsSlice.Events), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(result, CreateResolvedEvents(events));
            typeof(StreamEventsSlice)
                .GetField(nameof(StreamEventsSlice.IsEndOfStream), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(result, isEndOfStream);
            return result;
        }

        private static ResolvedEvent[] CreateResolvedEvents(object[] events)
        {
            return events
                .Select(x =>
                {
                    var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x, typeof(object), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }));

                    var @event = (RecordedEvent)FormatterServices.GetUninitializedObject(typeof(RecordedEvent));
                    typeof(RecordedEvent)
                        .GetField(nameof(RecordedEvent.Data), BindingFlags.Instance | BindingFlags.Public)
                        .SetValue(@event, data);

                    var resolvedEvent = (object)FormatterServices.GetUninitializedObject(typeof(ResolvedEvent));
                    typeof(ResolvedEvent)
                        .GetField(nameof(ResolvedEvent.Event), BindingFlags.Instance | BindingFlags.Public)
                        .SetValue(resolvedEvent, @event);
                    return (ResolvedEvent)resolvedEvent;
                })
                .ToArray();
        }

        private class EventA { }
        private class EventB { }
    }
}
