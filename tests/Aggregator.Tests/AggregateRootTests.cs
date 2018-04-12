using System.Linq;
using Aggregator.Exceptions;
using Aggregator.Internal;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests
{
    [TestFixture]
    public class AggregateRootTests
    {
        [Test]
        public void Initialize_PassEvents_ShouldHandleEvents()
        {
            var eventA = new EventA();
            var eventB = new EventB();
            var events = new object[] { eventA, eventB, eventA };
            var aggregateRootMock = new Mock<FakeAggregateRoot>();
            ((IAggregateRootInitializer<object>)aggregateRootMock.Object).Initialize(events);
            aggregateRootMock.Verify(x => x.OnEventA(eventA), Times.Exactly(2));
            aggregateRootMock.Verify(x => x.OnEventB(eventB), Times.Once);
        }

        [Test]
        public void Initialize_PassEvents_ShouldNotHaveChanges()
        {
            var events = new object[] { new EventA(), new EventB() };
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<object>)aggregateRoot).Initialize(events);
            var changeTracker = (IAggregateRootChangeTracker<object>)aggregateRoot;
            Assert.That(changeTracker.HasChanges, Is.False);
            Assert.That(changeTracker.GetChanges().Count(), Is.EqualTo(0));
        }

        [Test]
        public void Initialize_PassUnhandledEvent_ShouldThrowException()
        {
            var events = new object[] { new EventA(), new EventC() };
            IAggregateRootInitializer<object> aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var ex = Assert.Throws<UnhandledEventException>(() => aggregateRoot.Initialize(events));
            Assert.That(ex.Message, Is.EqualTo($"Unhandled event EventC"));
        }

        [Test]
        public void Register_RegisterForSameEventTwice_ShouldThrowException()
        {
            var ex = Assert.Throws<HandlerForEventAlreadyRegisteredException>(() => new RegisterTwiceAggregateRoot());
            Assert.That(ex.Message, Does.Contain("Handler for event EventA already registered"));
        }

        [Test]
        public void Apply_PassKnownEvent_ShouldHandleEvent()
        {
            var aggregateRootMock = new Mock<FakeAggregateRoot>();
            aggregateRootMock.Object.ApplyB();
            aggregateRootMock.Verify(x => x.OnEventB(It.IsAny<EventB>()), Times.Once);
        }

        [Test]
        public void Apply_PassKnownEvents_ShouldHaveChanges()
        {
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            aggregateRoot.ApplyBA();
            var changeTracker = (IAggregateRootChangeTracker<object>)aggregateRoot;
            Assert.That(changeTracker.HasChanges, Is.True);
            var changes = changeTracker.GetChanges().ToArray();
            Assert.That(changes, Has.Length.EqualTo(2));
            Assert.That(changes[0].GetType(), Is.EqualTo(typeof(EventB)));
            Assert.That(changes[1].GetType(), Is.EqualTo(typeof(EventA)));
        }

        [Test]
        public void Apply_UnhandledEvent_ShouldThrowException()
        {
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var ex = Assert.Throws<UnhandledEventException>(() => aggregateRoot.ApplyC());
            Assert.That(ex.Message, Is.EqualTo($"Unhandled event EventC"));
        }

        public abstract class FakeAggregateRoot : AggregateRoot<object>
        {
            public FakeAggregateRoot()
            {
                Register<EventA>(OnEventA);
                Register<EventB>(OnEventB);
            }

            public abstract void OnEventA(EventA @event);

            public abstract void OnEventB(EventB @event);

            public void ApplyB() => Apply(new EventB());

            public void ApplyBA()
            {
                Apply(new EventB());
                Apply(new EventA());
            }

            public void ApplyC() => Apply(new EventC());
        }

        public class EventA { }

        public class EventB { }

        public class EventC { }

        public class RegisterTwiceAggregateRoot : AggregateRoot<object>
        {
            public RegisterTwiceAggregateRoot()
            {
                Register<EventA>(_ => { });
                Register<EventA>(_ => { });
            }
        }
    }
}
