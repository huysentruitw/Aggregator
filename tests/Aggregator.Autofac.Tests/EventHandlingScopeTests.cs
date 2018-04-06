using System;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Event;
using Autofac;
using Moq;
using NUnit.Framework;

namespace Aggregator.Autofac.Tests
{
    [TestFixture]
    public class EventHandlingScopeTests
    {
        [Test]
        public void Contructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new EventHandlingScope<EventA>(null, Array.Empty<Type>()));
            Assert.That(ex.ParamName, Is.EqualTo("ownedLifetimeScope"));

            ex = Assert.Throws<ArgumentNullException>(() => new EventHandlingScope<EventA>(new Mock<ILifetimeScope>().Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("handlerTypes"));
        }

        [Test]
        public void Dispose_ShouldDisposeLifetimeScope()
        {
            var lifetimeScopeMock = new Mock<ILifetimeScope>();
            var scope = new EventHandlingScope<EventA>(lifetimeScopeMock.Object, Array.Empty<Type>());
            lifetimeScopeMock.Verify(x => x.Dispose(), Times.Never);
            scope.Dispose();
            lifetimeScopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ResolveHandlers_ShouldResolveHandlersFromLifetimeScope()
        {
            var handlerTypes = new[]
            {
                typeof(EventHandler1),
                typeof(EventHandler2)
            };

            var container = handlerTypes.Aggregate(new ContainerBuilder(), (builder, type) =>
            {
                builder.RegisterType(type);
                return builder;
            }).Build();

            var scope = new EventHandlingScope<EventA>(container.BeginLifetimeScope(), handlerTypes);
            var handlers = scope.ResolveHandlers();

            Assert.That(handlers, Has.Length.EqualTo(2));
            Assert.That(handlers[0].GetType(), Is.EqualTo(typeof(EventHandler1)));
            Assert.That(handlers[1].GetType(), Is.EqualTo(typeof(EventHandler2)));
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
