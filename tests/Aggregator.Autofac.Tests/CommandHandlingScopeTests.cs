using System;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Command;
using Autofac;
using Moq;
using NUnit.Framework;

namespace Aggregator.Autofac.Tests
{
    [TestFixture]
    public class CommandHandlingScopeTests
    {
        [Test]
        public void Contructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandlingScope<CommandA>(null, Array.Empty<Type>()));
            Assert.That(ex.ParamName, Is.EqualTo("ownedLifetimeScope"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandHandlingScope<CommandA>(new Mock<ILifetimeScope>().Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("handlerTypes"));
        }

        [Test]
        public void Dispose_ShouldDisposeLifetimeScope()
        {
            var lifetimeScopeMock = new Mock<ILifetimeScope>();
            var scope = new CommandHandlingScope<CommandA>(lifetimeScopeMock.Object, Array.Empty<Type>());
            lifetimeScopeMock.Verify(x => x.Dispose(), Times.Never);
            scope.Dispose();
            lifetimeScopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ResolveHandlers_ShouldResolveHandlersFromLifetimeScope()
        {
            var handlerTypes = new[]
            {
                typeof(CommandHandler1),
                typeof(CommandHandler2)
            };

            var container = handlerTypes.Aggregate(new ContainerBuilder(), (builder, type) =>
            {
                builder.RegisterType(type);
                return builder;
            }).Build();

            var scope = new CommandHandlingScope<CommandA>(container.BeginLifetimeScope(), handlerTypes);
            var handlers = scope.ResolveHandlers();

            Assert.That(handlers, Has.Length.EqualTo(2));
            Assert.That(handlers[0].GetType(), Is.EqualTo(typeof(CommandHandler1)));
            Assert.That(handlers[1].GetType(), Is.EqualTo(typeof(CommandHandler2)));
        }

        public class CommandA { }

        public class CommandHandler1 : ICommandHandler<CommandA>
        {
            public Task Handle(CommandA command) => Task.CompletedTask;
        }

        public class CommandHandler2 : ICommandHandler<CommandA>
        {
            public Task Handle(CommandA command) => Task.CompletedTask;
        }
    }
}
