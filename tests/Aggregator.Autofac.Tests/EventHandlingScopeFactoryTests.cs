using System;
using System.Threading.Tasks;
using Aggregator.Event;
using Autofac;
using Moq;
using NUnit.Framework;

namespace Aggregator.Autofac.Tests
{
    [TestFixture]
    public class EventHandlingScopeFactoryTests
    {
        private readonly Mock<IEventHandlerTypeLocator> _eventHandlerTypeLocatorMock = new Mock<IEventHandlerTypeLocator>();
        private readonly Mock<ILifetimeScope> _lifetimeScopeMock = new Mock<ILifetimeScope>();

        [SetUp]
        public void SetUp()
        {
            _eventHandlerTypeLocatorMock.Reset();
            _lifetimeScopeMock.Reset();
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new EventHandlingScopeFactory(null, _lifetimeScopeMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("eventHandlerTypeLocator"));

            ex = Assert.Throws<ArgumentNullException>(() => new EventHandlingScopeFactory(_eventHandlerTypeLocatorMock.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("lifetimeScope"));
        }

        [Test]
        public void BeginScopeFor_ShouldRequestHandlerTypes()
        {
            var innerLifetimeScopeMock = new Mock<ILifetimeScope>();
            _lifetimeScopeMock
                .Setup(x => x.BeginLifetimeScope(It.IsAny<Action<ContainerBuilder>>()))
                .Returns(innerLifetimeScopeMock.Object);
            var factory = new EventHandlingScopeFactory(_eventHandlerTypeLocatorMock.Object, _lifetimeScopeMock.Object);
            factory.BeginScopeFor<EventA>();
            _eventHandlerTypeLocatorMock.Verify(x => x.For<EventA>(), Times.Once);
        }

        [Test]
        public void BeginScopeFor_ShouldRegisterHandlerTypesOnInnerScope()
        {
            Action<ContainerBuilder> containerBuilderAction = null;
            var innerLifetimeScopeMock = new Mock<ILifetimeScope>();
            _lifetimeScopeMock
                .Setup(x => x.BeginLifetimeScope(It.IsAny<Action<ContainerBuilder>>()))
                .Callback<Action<ContainerBuilder>>(action => containerBuilderAction = action)
                .Returns(innerLifetimeScopeMock.Object);

            _eventHandlerTypeLocatorMock
                .Setup(x => x.For<EventA>())
                .Returns(new[]
                {
                    typeof(EventHandler1),
                    typeof(EventHandler2)
                });

            var factory = new EventHandlingScopeFactory(_eventHandlerTypeLocatorMock.Object, _lifetimeScopeMock.Object);
            factory.BeginScopeFor<EventA>();

            Assert.That(containerBuilderAction, Is.Not.Null);
            var builder = new ContainerBuilder();
            containerBuilderAction.Invoke(builder);
            var container = builder.Build();
            Assert.That(container.Resolve<EventHandler1>(), Is.Not.Null);
            Assert.That(container.Resolve<EventHandler2>(), Is.Not.Null);
        }

        public class EventA { }

        public class EventHandler1 : IEventHandler<EventA>
        {
            public Task Handle(EventA @event) => Task.CompletedTask;
        }

        public class EventHandler2 : IEventHandler<EventA>
        {
            public Task Handle(EventA @event) => Task.CompletedTask;
        }
    }
}
