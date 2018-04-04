using System;
using System.Threading.Tasks;
using Aggregator.Command;
using Autofac;
using Moq;
using NUnit.Framework;

namespace Aggregator.Autofac.Tests
{
    [TestFixture]
    public class CommandHandlingScopeFactoryTests
    {
        private readonly Mock<ICommandHandlerTypeLocator> _commandHandlerTypeLocatorMock = new Mock<ICommandHandlerTypeLocator>();
        private readonly Mock<ILifetimeScope> _lifetimeScopeMock = new Mock<ILifetimeScope>();

        [SetUp]
        public void SetUp()
        {
            _commandHandlerTypeLocatorMock.Reset();
            _lifetimeScopeMock.Reset();
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandlingScopeFactory(null, _lifetimeScopeMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("commandHandlerTypeLocator"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandHandlingScopeFactory(_commandHandlerTypeLocatorMock.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("lifetimeScope"));
        }

        [Test]
        public void BeginScopeFor_PassNullAsContext_ShouldThrowException()
        {
            var factory = new CommandHandlingScopeFactory(_commandHandlerTypeLocatorMock.Object, _lifetimeScopeMock.Object);
            var ex = Assert.Throws<ArgumentNullException>(() => factory.BeginScopeFor<CommandA>(null));
            Assert.That(ex.ParamName, Is.EqualTo("context"));
        }

        [Test]
        public void BeginScopeFor_ShouldRequestHandlerTypes()
        {
            var innerLifetimeScopeMock = new Mock<ILifetimeScope>();
            _lifetimeScopeMock
                .Setup(x => x.BeginLifetimeScope(It.IsAny<Action<ContainerBuilder>>()))
                .Returns(innerLifetimeScopeMock.Object);
            var context = new CommandHandlingContext();
            var factory = new CommandHandlingScopeFactory(_commandHandlerTypeLocatorMock.Object, _lifetimeScopeMock.Object);
            factory.BeginScopeFor<CommandA>(context);
            _commandHandlerTypeLocatorMock.Verify(x => x.For<CommandA>(), Times.Once);
        }

        [Test]
        public void BeginScopeFor_ShouldRegisterContextOnInnerScope()
        {
            Action<ContainerBuilder> containerBuilderAction = null;
            var innerLifetimeScopeMock = new Mock<ILifetimeScope>();
            _lifetimeScopeMock
                .Setup(x => x.BeginLifetimeScope(It.IsAny<Action<ContainerBuilder>>()))
                .Callback<Action<ContainerBuilder>>(action => containerBuilderAction = action)
                .Returns(innerLifetimeScopeMock.Object);

            var context = new CommandHandlingContext();
            var factory = new CommandHandlingScopeFactory(_commandHandlerTypeLocatorMock.Object, _lifetimeScopeMock.Object);
            factory.BeginScopeFor<CommandA>(context);

            Assert.That(containerBuilderAction, Is.Not.Null);
            var builder = new ContainerBuilder();
            containerBuilderAction.Invoke(builder);
            var container = builder.Build();
            Assert.That(container.Resolve<CommandHandlingContext>(), Is.EqualTo(context));
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

            _commandHandlerTypeLocatorMock
                .Setup(x => x.For<CommandA>())
                .Returns(new[]
                {
                    typeof(CommandHandler1),
                    typeof(CommandHandler2)
                });

            var context = new CommandHandlingContext();
            var factory = new CommandHandlingScopeFactory(_commandHandlerTypeLocatorMock.Object, _lifetimeScopeMock.Object);
            factory.BeginScopeFor<CommandA>(context);

            Assert.That(containerBuilderAction, Is.Not.Null);
            var builder = new ContainerBuilder();
            containerBuilderAction.Invoke(builder);
            var container = builder.Build();
            Assert.That(container.Resolve<CommandHandler1>(), Is.Not.Null);
            Assert.That(container.Resolve<CommandHandler2>(), Is.Not.Null);
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
