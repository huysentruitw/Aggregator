using System.Threading.Tasks;
using Aggregator.Command;
using NUnit.Framework;

namespace Aggregator.Tests.Command
{
    [TestFixture]
    public class ReflectionCommandHandlerTypeLocatorTests
    {
        [Test]
        public void For_UnhandledCommand_ShouldReturnEmptyArray()
        {
            var locator = new ReflectionCommandHandlerTypeLocator();
            var result = locator.For<UnhandledCommand>();
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void For_CommandHandledBySingleHandler_ShouldReturnCommandHandlerType()
        {
            var locator = new ReflectionCommandHandlerTypeLocator();
            var result = locator.For<CommandHandledBySingleHandler>();
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(typeof(SingleHandler)));
        }

        [Test]
        public void For_CommandHandledBySharedHandler_ShouldReturnCommandHandlerType()
        {
            var locator = new ReflectionCommandHandlerTypeLocator();
            var result = locator.For<CommandHandlerdBySharedHandler1>();
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(typeof(SharedHandler)));
        }

        [Test]
        public void For_CommandHandledByMultipleHandlers_ShouldReturnMatchingCommandHandlerTypes()
        {
            var locator = new ReflectionCommandHandlerTypeLocator();
            var result = locator.For<CommandHandledByMultipleHandlers>();
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result, Contains.Item(typeof(MultipleHandler1)));
            Assert.That(result, Contains.Item(typeof(MultipleHandler2)));
        }

        public class UnhandledCommand { }

        public class CommandHandledBySingleHandler { }

        public class SingleHandler : ICommandHandler<CommandHandledBySingleHandler>
        {
            public Task Handle(CommandHandledBySingleHandler command) => Task.CompletedTask;
        }

        public class CommandHandlerdBySharedHandler1 { }

        public class CommandHandlerdBySharedHandler2 { }

        public class SharedHandler : ICommandHandler<CommandHandlerdBySharedHandler1>, ICommandHandler<CommandHandlerdBySharedHandler2>
        {
            public Task Handle(CommandHandlerdBySharedHandler1 command) => Task.CompletedTask;

            public Task Handle(CommandHandlerdBySharedHandler2 command) => Task.CompletedTask;
        }

        public class CommandHandledByMultipleHandlers { }

        public class MultipleHandler1 : ICommandHandler<CommandHandledByMultipleHandlers>
        {
            public Task Handle(CommandHandledByMultipleHandlers command) => Task.CompletedTask;
        }

        public class MultipleHandler2 : ICommandHandler<CommandHandledByMultipleHandlers>
        {
            public Task Handle(CommandHandledByMultipleHandlers command) => Task.CompletedTask;
        }
    }
}
