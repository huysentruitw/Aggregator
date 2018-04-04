using System;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.Exceptions;
using Aggregator.Persistence;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Command
{
    [TestFixture]
    public class CommandProcessorTests
    {
        private readonly Mock<ICommandHandlingScopeFactory> _commandHandlingScopeFactory = new Mock<ICommandHandlingScopeFactory>();
        private readonly Mock<ICommandHandlingScope<CommandA>> _commandHandlingScopeAMock = new Mock<ICommandHandlingScope<CommandA>>();
        private readonly Mock<ICommandHandlingScope<CommandB>> _commandHandlingScopeBMock = new Mock<ICommandHandlingScope<CommandB>>();
        private readonly Mock<IEventStore<string, object>> _eventStoreMock = new Mock<IEventStore<string, object>>();

        [SetUp]
        public void SetUp()
        {
            _commandHandlingScopeFactory.Reset();
            _commandHandlingScopeAMock.Reset();
            _commandHandlingScopeBMock.Reset();
            _eventStoreMock.Reset();

            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns(_commandHandlingScopeAMock.Object);

            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandB>(It.IsAny<CommandHandlingContext>()))
                .Returns(_commandHandlingScopeBMock.Object);
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor<string, object, object>(null, _eventStoreMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("commandHandlingScopeFactory"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor<string, object, object>(_commandHandlingScopeFactory.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("eventStore"));
        }

        [Test]
        public void Process_PassNullAsCommand_ShouldThrowException()
        {
            var processor = new CommandProcessor<string, object, object>(_commandHandlingScopeFactory.Object, _eventStoreMock.Object);
            var ex = Assert.ThrowsAsync<ArgumentNullException>(() => processor.Process(null));
            Assert.That(ex.ParamName, Is.EqualTo("command"));
        }

        [Test]
        public async Task Process_PassCommand_ShouldCreateCommandHandlingScope()
        {
            _commandHandlingScopeAMock.Setup(x => x.ResolveHandlers()).Returns(new[] { new Mock<ICommandHandler<CommandA>>().Object });
            var processor = new CommandProcessor<string, object, object>(_commandHandlingScopeFactory.Object, _eventStoreMock.Object);
            await processor.Process(new CommandA());
            _commandHandlingScopeFactory.Verify(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()), Times.Once);
        }

        [Test]
        public async Task Process_PassCommand_ShouldResolveHandlersInScope()
        {
            _commandHandlingScopeAMock.Setup(x => x.ResolveHandlers()).Returns(new[] { new Mock<ICommandHandler<CommandA>>().Object });
            var processor = new CommandProcessor<string, object, object>(_commandHandlingScopeFactory.Object, _eventStoreMock.Object);
            await processor.Process(new CommandA());
            _commandHandlingScopeAMock.Verify(x => x.ResolveHandlers(), Times.Once);
        }

        [Test]
        public async Task Process_PassCommand_ShouldForwardCommandToHandlers()
        {
            var command = new CommandA();

            var handlerMocks = new[]
            {
                new Mock<ICommandHandler<CommandA>>(),
                new Mock<ICommandHandler<CommandA>>()
            };

            _commandHandlingScopeAMock.Setup(x => x.ResolveHandlers())
                .Returns(handlerMocks.Select(x => x.Object).ToArray());

            var processor = new CommandProcessor<string, object, object>(_commandHandlingScopeFactory.Object, _eventStoreMock.Object);
            await processor.Process(command);

            handlerMocks[0].Verify(x => x.Handle(command), Times.Once);
            handlerMocks[1].Verify(x => x.Handle(command), Times.Once);
        }

        [Test]
        public void Process_PassUnhandledCommand_ShouldThrowException()
        {
            var command = new CommandB();
            var processor = new CommandProcessor<string, object, object>(_commandHandlingScopeFactory.Object, _eventStoreMock.Object);
            var ex = Assert.ThrowsAsync<UnhandledCommandException>(() => processor.Process(command));
            Assert.That(ex.Command, Is.EqualTo(command));
            Assert.That(ex.CommandType, Is.EqualTo(typeof(CommandB)));
            Assert.That(ex.Message, Is.EqualTo("Unhandled command 'CommandB'"));
        }

        public class CommandA { }

        public class CommandB { }
    }
}
