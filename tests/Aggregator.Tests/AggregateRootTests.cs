using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Aggregator.Exceptions;
using Aggregator.Internal;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Tests
{
    public sealed class AggregateRootTests
    {
        [Fact]
        public void Initialize_PassNullAsEvents_ShouldNotThrowException()
        {
            // Arrange
            IAggregateRootInitializer<object> aggregateRoot = new Mock<FakeAggregateRoot>().Object;

            // Act & Assert
            Action action = () => aggregateRoot.Initialize(null);
            action.Should().NotThrow();
        }

        [Fact]
        public void Initialize_PassEvents_ShouldHandleEvents()
        {
            // Arrange
            var eventA = new EventA();
            var eventB = new EventB();
            var events = new object[] { eventA, eventB, eventA };
            var aggregateRootMock = new Mock<FakeAggregateRoot>();

            // Act
            ((IAggregateRootInitializer<object>)aggregateRootMock.Object).Initialize(events);

            // Assert
            aggregateRootMock.Verify(x => x.OnEventA(eventA), Times.Exactly(2));
            aggregateRootMock.Verify(x => x.OnEventB(eventB), Times.Once);
        }

        [Fact]
        public void Initialize_PassEvents_ShouldNotHaveChanges()
        {
            // Arrange
            var events = new object[] { new EventA(), new EventB() };
            AggregateRoot aggregateRoot = Mock.Of<FakeAggregateRoot>();

            // Act
            ((IAggregateRootInitializer<object>)aggregateRoot).Initialize(events);

            // Assert
            var changeTracker = (IAggregateRootChangeTracker<object>)aggregateRoot;
            changeTracker.HasChanges.Should().BeFalse();
            changeTracker.GetChanges().Should().HaveCount(0);
        }

        [Fact]
        public void Initialize_PassUnhandledEvent_ShouldThrowException()
        {
            // Assert
            var events = new object[] { new EventA(), new EventC() };
            IAggregateRootInitializer<object> aggregateRoot = new Mock<FakeAggregateRoot>().Object;

            // Act & Assert
            Action action = () => aggregateRoot.Initialize(events);
            action.Should().Throw<UnhandledEventException>()
                .WithMessage("Unhandled event EventC");
        }

        [Fact]
        public void Register_RegisterForSameEventTwice_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => new RegisterTwiceAggregateRoot();
            action.Should().Throw<HandlerForEventAlreadyRegisteredException>()
                .WithMessage("*Handler for event EventA already registered*");
        }

        [Fact]
        public void Register_PassNullAsHandler_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => new RegisterNullAggregateRoot();
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("handler");
        }

        [Fact]
        public void Apply_PassNullAsEvent_ShouldThrowException()
        {
            // Arrange
            FakeAggregateRoot aggregateRoot = Mock.Of<FakeAggregateRoot>();

            // Act & Assert
            Action action = () => aggregateRoot.ApplyNull();
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("event");
        }

        [Fact]
        public void Apply_PassKnownEvent_ShouldHandleEvent()
        {
            // Arrange
            var aggregateRootMock = new Mock<FakeAggregateRoot>();

            // Act
            aggregateRootMock.Object.ApplyB();

            // Assert
            aggregateRootMock.Verify(x => x.OnEventB(It.IsAny<EventB>()), Times.Once);
        }

        [Fact]
        public void Apply_PassKnownEvents_ShouldHaveChanges()
        {
            // Arrange
            FakeAggregateRoot aggregateRoot = Mock.Of<FakeAggregateRoot>();

            // Act
            aggregateRoot.ApplyBA();

            // Assert
            var changeTracker = (IAggregateRootChangeTracker<object>)aggregateRoot;
            changeTracker.HasChanges.Should().BeTrue();
            object[] changes = changeTracker.GetChanges().ToArray();
            changes.Should().HaveCount(2);
            changes[0].Should().BeOfType<EventB>();
            changes[1].Should().BeOfType<EventA>();
        }

        [Fact]
        public void Apply_UnhandledEvent_ShouldThrowException()
        {
            // Arrange
            FakeAggregateRoot aggregateRoot = Mock.Of<FakeAggregateRoot>();

            // Act & Assert
            Action action = () => aggregateRoot.ApplyC();
            action.Should().Throw<UnhandledEventException>()
                .WithMessage("Unhandled event EventC");
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Needs to be public for Mock")]
        public abstract class FakeAggregateRoot : AggregateRoot
        {
            protected FakeAggregateRoot()
            {
                Register<EventA>(OnEventA);
                Register<EventB>(OnEventB);
            }

            public abstract void OnEventA(EventA @event);

            public abstract void OnEventB(EventB @event);

            public void ApplyB() => Apply(new EventB());

            // ReSharper disable once InconsistentNaming
            public void ApplyBA()
            {
                Apply(new EventB());
                Apply(new EventA());
            }

            public void ApplyC() => Apply(new EventC());

            public void ApplyNull() => Apply((EventA)null);
        }

        public sealed class EventA
        {
        }

        public sealed class EventB
        {
        }

        private sealed class EventC
        {
        }

        private sealed class RegisterTwiceAggregateRoot : AggregateRoot
        {
            public RegisterTwiceAggregateRoot()
            {
                Register<EventA>(_ => { });
                Register<EventA>(_ => { });
            }
        }

        private sealed class RegisterNullAggregateRoot : AggregateRoot
        {
            public RegisterNullAggregateRoot()
            {
                Register<EventA>(null);
            }
        }
    }
}
