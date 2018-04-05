using System.Threading.Tasks;
using Aggregator.Event;
using NUnit.Framework;

namespace Aggregator.Tests.Event
{
    [TestFixture]
    public class ReflectionEventHandlerTypeLocatorTests
    {
        [Test]
        public void For_UnhandledEvent_ShouldReturnEmptyArray()
        {
            var locator = new ReflectionEventHandlerTypeLocator();
            var result = locator.For<UnhandledEvent>();
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void For_EventHandledBySingleHandler_ShouldReturnEventHandlerType()
        {
            var locator = new ReflectionEventHandlerTypeLocator();
            var result = locator.For<EventHandledBySingleHandler>();
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(typeof(SingleHandler)));
        }

        [Test]
        public void For_EventHandledBySharedHandler_ShouldReturnEventHandlerType()
        {
            var locator = new ReflectionEventHandlerTypeLocator();
            var result = locator.For<EventHandlerdBySharedHandler1>();
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(typeof(SharedHandler)));
        }

        [Test]
        public void For_EventHandledByMultipleHandlers_ShouldReturnMatchingEventHandlerTypes()
        {
            var locator = new ReflectionEventHandlerTypeLocator();
            var result = locator.For<EventHandledByMultipleHandlers>();
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result, Contains.Item(typeof(MultipleHandler1)));
            Assert.That(result, Contains.Item(typeof(MultipleHandler2)));
        }

        public class UnhandledEvent { }

        public class EventHandledBySingleHandler { }

        public class SingleHandler : IEventHandler<EventHandledBySingleHandler>
        {
            public Task Handle(EventHandledBySingleHandler @event) => Task.CompletedTask;
        }

        public class EventHandlerdBySharedHandler1 { }

        public class EventHandlerdBySharedHandler2 { }

        public class SharedHandler : IEventHandler<EventHandlerdBySharedHandler1>, IEventHandler<EventHandlerdBySharedHandler2>
        {
            public Task Handle(EventHandlerdBySharedHandler1 @event) => Task.CompletedTask;

            public Task Handle(EventHandlerdBySharedHandler2 @event) => Task.CompletedTask;
        }

        public class EventHandledByMultipleHandlers { }

        public class MultipleHandler1 : IEventHandler<EventHandledByMultipleHandlers>
        {
            public Task Handle(EventHandledByMultipleHandlers @event) => Task.CompletedTask;
        }

        public class MultipleHandler2 : IEventHandler<EventHandledByMultipleHandlers>
        {
            public Task Handle(EventHandledByMultipleHandlers @event) => Task.CompletedTask;
        }
    }
}
