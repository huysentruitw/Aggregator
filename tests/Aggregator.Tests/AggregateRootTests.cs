using System;
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
        public void Initialize_PassDefaultIdentifier_ShouldThrowException()
        {
            IAggregateRootInitializer<string, object> aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var ex = Assert.Throws<ArgumentException>(() => aggregateRoot.Initialize(default(string), 1));
            Assert.That(ex.ParamName, Is.EqualTo("identifier"));
            Assert.That(ex.Message, Does.StartWith("Default value not allowed"));
        }

        [Test]
        public void Initialize_PassIdentifier_ShouldSetIdentifierProperty()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(identifier, 1);
            Assert.That(aggregateRoot.Identifier, Is.EqualTo(identifier));
        }

        [Test]
        public void Initialize_PassExpectedVersion_ShouldSetExpectedVersionProperty()
        {
            var expectedVersion = 13;
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize("some_id", expectedVersion);
            Assert.That(aggregateRoot.ExpectedVersion, Is.EqualTo(expectedVersion));
        }

        [Test]
        public void Initialize_PassEvents_ShouldHandleEvents()
        {
            var eventA = new EventA();
            var eventB = new EventB();
            var events = new object[] { eventA, eventB, eventA };
            var aggregateRootMock = new Mock<FakeAggregateRoot>();
            ((IAggregateRootInitializer<string, object>)aggregateRootMock.Object).Initialize("some_id", 3, events);
            aggregateRootMock.Verify(x => x.OnEventA(eventA), Times.Exactly(2));
            aggregateRootMock.Verify(x => x.OnEventB(eventB), Times.Once);
        }

        [Test]
        public void Initialize_PassEvents_ShouldNotHaveChanges()
        {
            var events = new object[] { new EventA(), new EventB() };
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize("some_id", 3, events);
            var changeTracker = (IAggregateRootChangeTracker<string, object>)aggregateRoot;
            Assert.That(changeTracker.HasChanges, Is.False);
            Assert.That(changeTracker.GetChanges().Count(), Is.EqualTo(0));
        }

        [Test]
        public void Initialize_InitializeTwice_ShouldThrowException()
        {
            var events = new object[] { new EventA(), new EventB() };
            IAggregateRootInitializer<string, object> aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            aggregateRoot.Initialize("some_id", 3, events);
            var ex = Assert.Throws<InvalidOperationException>(() => aggregateRoot.Initialize("some_id", 3, events));
            Assert.That(ex.Message, Is.EqualTo("Already initialized"));
        }

        [Test]
        public void Initialize_PassUnhandledEvent_ShouldThrowException()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var events = new object[] { new EventA(), new EventC() };
            IAggregateRootInitializer<string, object> aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var ex = Assert.Throws<UnhandledEventException<string>>(() => aggregateRoot.Initialize(identifier, 3, events));
            Assert.That(ex.Identifier, Is.EqualTo(identifier));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{identifier}': Unhandled event EventC"));
        }

        [Test]
        public void Register_RegisterForSameEventTwice_ShouldThrowException()
        {
            var ex = Assert.Throws<HandlerForEventAlreadyRegisteredException<string>>(() => new RegisterTwiceAggregateRoot());
            Assert.That(ex.Message, Does.Contain("Handler for event EventA already registered"));
        }

        [Test]
        public void Apply_PassKnownEvent_ShouldHandleEvent()
        {
            var aggregateRootMock = new Mock<FakeAggregateRoot>();
            ((IAggregateRootInitializer<string, object>)aggregateRootMock.Object).Initialize("some_id", 1);
            aggregateRootMock.Object.ApplyB();
            aggregateRootMock.Verify(x => x.OnEventB(It.IsAny<EventB>()), Times.Once);
        }

        [Test]
        public void Apply_PassKnownEvents_ShouldHaveChanges()
        {
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize("some_id", 1);
            aggregateRoot.ApplyBA();
            var changeTracker = (IAggregateRootChangeTracker<string, object>)aggregateRoot;
            Assert.That(changeTracker.HasChanges, Is.True);
            var changes = changeTracker.GetChanges().ToArray();
            Assert.That(changes, Has.Length.EqualTo(2));
            Assert.That(changes[0].GetType(), Is.EqualTo(typeof(EventB)));
            Assert.That(changes[1].GetType(), Is.EqualTo(typeof(EventA)));
        }

        [Test]
        public void Apply_UnhandledEvent_ShouldThrowException()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(identifier, 1);
            var ex = Assert.Throws<UnhandledEventException<string>>(() => aggregateRoot.ApplyC());
            Assert.That(ex.Identifier, Is.EqualTo(identifier));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{identifier}': Unhandled event EventC"));
        }

        [Test]
        public void Apply_AggregateRootNotInitialized_ShouldThrowException()
        {
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var ex = Assert.Throws<InvalidOperationException>(() => aggregateRoot.ApplyB());
            Assert.That(ex.Message, Is.EqualTo("AggregateRoot not initialized"));
        }

        [Test]
        public void GetChanges_AggregateRootNotInitialized_ShouldThrowException()
        {
            IAggregateRootChangeTracker<string, object> aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var ex = Assert.Throws<InvalidOperationException>(() => aggregateRoot.GetChanges());
            Assert.That(ex.Message, Is.EqualTo("AggregateRoot not initialized"));
        }

        public abstract class FakeAggregateRoot : AggregateRoot<string, object>
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

        public class RegisterTwiceAggregateRoot : AggregateRoot<string, object>
        {
            public RegisterTwiceAggregateRoot()
            {
                Register<EventA>(_ => { });
                Register<EventA>(_ => { });
            }
        }
    }
}
