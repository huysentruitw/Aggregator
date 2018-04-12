using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.Event;
using Aggregator.Exceptions;
using Aggregator.Internal;
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
        private readonly Mock<IEventDispatcher<object>> _eventDispatcherMock = new Mock<IEventDispatcher<object>>();
        private readonly Mock<IEventStore<string, object>> _eventStoreMock = new Mock<IEventStore<string, object>>();

        [SetUp]
        public void SetUp()
        {
            _commandHandlingScopeFactory.Reset();
            _commandHandlingScopeAMock.Reset();
            _commandHandlingScopeBMock.Reset();
            _eventDispatcherMock.Reset();
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
            var ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(null, _eventDispatcherMock.Object, _eventStoreMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("commandHandlingScopeFactory"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(_commandHandlingScopeFactory.Object, null, _eventStoreMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("eventDispatcher"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("eventStore"));
        }

        [Test]
        public void Process_PassNullAsCommand_ShouldThrowException()
        {
            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            var ex = Assert.ThrowsAsync<ArgumentNullException>(() => processor.Process(null));
            Assert.That(ex.ParamName, Is.EqualTo("command"));
        }

        [Test]
        public async Task Process_PassCommand_ShouldCreateCommandHandlingScope()
        {
            _commandHandlingScopeAMock.Setup(x => x.ResolveHandlers()).Returns(new[] { new Mock<ICommandHandler<CommandA>>().Object });
            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            await processor.Process(new CommandA());
            _commandHandlingScopeFactory.Verify(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()), Times.Once);
        }

        [Test]
        public async Task Process_PassCommand_ShouldResolveHandlersInScope()
        {
            _commandHandlingScopeAMock.Setup(x => x.ResolveHandlers()).Returns(new[] { new Mock<ICommandHandler<CommandA>>().Object });
            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
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

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            await processor.Process(command);

            handlerMocks[0].Verify(x => x.Handle(command), Times.Once);
            handlerMocks[1].Verify(x => x.Handle(command), Times.Once);
        }

        [Test]
        public void Process_PassUnhandledCommand_ShouldThrowException()
        {
            var command = new CommandB();
            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            var ex = Assert.ThrowsAsync<UnhandledCommandException>(() => processor.Process(command));
            Assert.That(ex.Command, Is.EqualTo(command));
            Assert.That(ex.CommandType, Is.EqualTo(typeof(CommandB)));
            Assert.That(ex.Message, Is.EqualTo("Unhandled command 'CommandB'"));
        }

        [Test]
        public async Task Process_NoChangedAggregateRootsInUnitOfWork_ShouldNotBeginEventStoreTransaction()
        {
            var scopeMock = new Mock<ICommandHandlingScope<CommandA>>();
            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns(scopeMock.Object);

            scopeMock
                .Setup(x => x.ResolveHandlers())
                .Returns(new[] { new Mock<ICommandHandler<CommandA>>().Object });

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            await processor.Process(new CommandA());
            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Never);
        }

        [Test]
        public async Task Process_ChangedAggregateRootsInUnitOfWork_ShouldCommitChangesToEventStore()
        {
            object[] capturedEvents = null;

            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>()))
                .Callback<string, long, IEnumerable<object>>((_, __, events) => capturedEvents = events.ToArray())
                .Returns(Task.CompletedTask);

            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);

            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns<CommandHandlingContext>(context => new FakeCommandHandlingScope(context, x => x.DoSomething()));

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            await processor.Process(new CommandA());

            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);
            Assert.That(capturedEvents, Is.Not.Null);
            Assert.That(capturedEvents, Has.Length.EqualTo(2));
            Assert.That(capturedEvents[0], Is.InstanceOf<FakeEvent2>());
            Assert.That(capturedEvents[1], Is.InstanceOf<FakeEvent1>());

            transactionMock.Verify(x => x.Commit(), Times.Once);
            transactionMock.Verify(x => x.Rollback(), Times.Never);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Process_ApplyingEventToAggregateThrowsException_ShouldNotBeginTransaction()
        {
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();

            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);

            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns<CommandHandlingContext>(context => new FakeCommandHandlingScope(context, x => x.DoSomethingBad()));

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            Assert.ThrowsAsync<KeyNotFoundException>(() => processor.Process(new CommandA()));

            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Never);
        }

        [Test]
        public void Process_StoreEventsThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>()))
                .Callback(() => throw new InvalidOperationException("StoreEvents failed"));

            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);

            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns<CommandHandlingContext>(context => new FakeCommandHandlingScope(context, x => x.DoSomething()));

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => processor.Process(new CommandA()));
            Assert.That(ex.Message, Is.EqualTo("StoreEvents failed"));

            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Once);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Process_DispatchThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            _eventDispatcherMock
                .Setup(x => x.Dispatch(It.IsAny<object[]>()))
                .Callback(() => throw new InvalidOperationException("Dispatch failed"));

            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);

            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns<CommandHandlingContext>(context => new FakeCommandHandlingScope(context, x => x.DoSomething()));

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => processor.Process(new CommandA()));
            Assert.That(ex.Message, Is.EqualTo("Dispatch failed"));

            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Once);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task Process_NoChangedAggregateRootsInUnitOfWork_ShouldDispatchEmptyEventsArray()
        {
            var scopeMock = new Mock<ICommandHandlingScope<CommandA>>();
            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns(scopeMock.Object);

            scopeMock
                .Setup(x => x.ResolveHandlers())
                .Returns(new[] { new Mock<ICommandHandler<CommandA>>().Object });

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            await processor.Process(new CommandA());
            _eventDispatcherMock.Verify(x => x.Dispatch(Array.Empty<object>()), Times.Once);
        }

        [Test]
        public async Task Process_ChangedAggregateRootsInUnitOfWork_ShouldDispatchEvents()
        {
            object[] dispatchedEvents = null;
            _eventDispatcherMock
                .Setup(x => x.Dispatch(It.IsAny<object[]>()))
                .Callback<object[]>(events => dispatchedEvents = events)
                .Returns(Task.CompletedTask);

            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();

            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);

            _commandHandlingScopeFactory
                .Setup(x => x.BeginScopeFor<CommandA>(It.IsAny<CommandHandlingContext>()))
                .Returns<CommandHandlingContext>(context => new FakeCommandHandlingScope(context, x => x.DoSomething()));

            var processor = new CommandProcessor(_commandHandlingScopeFactory.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            await processor.Process(new CommandA());

            _eventDispatcherMock.Verify(x => x.Dispatch(It.IsAny<object[]>()), Times.Once);
            Assert.That(dispatchedEvents, Is.Not.Null);
            Assert.That(dispatchedEvents, Has.Length.EqualTo(2));
            Assert.That(dispatchedEvents[0], Is.InstanceOf<FakeEvent2>());
            Assert.That(dispatchedEvents[1], Is.InstanceOf<FakeEvent1>());
        }

        #region Test infrastructure

        public class CommandA { }

        public class CommandB { }

        private class FakeCommandHandlingScope : ICommandHandlingScope<CommandA>
        {
            private readonly CommandHandlingContext _context;
            private readonly Action<FakeAggregateRoot> _action;

            public FakeCommandHandlingScope(CommandHandlingContext context, Action<FakeAggregateRoot> action)
            {
                _context = context;
                _action = action;
            }

            public void Dispose() { }

            public ICommandHandler<CommandA>[] ResolveHandlers()
                => new[] { new FakeCommandHandler(_context, _action) };
        }

        private class FakeCommandHandler : ICommandHandler<CommandA>
        {
            private readonly CommandHandlingContext _context;
            private readonly Action<FakeAggregateRoot> _action;

            public FakeCommandHandler(CommandHandlingContext context, Action<FakeAggregateRoot> action)
            {
                _context = context;
                _action = action;
            }

            public Task Handle(CommandA command)
            {
                var unitOfWork = _context.GetUnitOfWork<string, object>();
                var aggregateRoot = new FakeAggregateRoot();
                unitOfWork.Attach(new AggregateRootEntity<string, object>("some_id", aggregateRoot, 5));
                _action.Invoke(aggregateRoot);
                return Task.CompletedTask;
            }
        }

        private class FakeAggregateRoot : AggregateRoot<object>
        {
            public FakeAggregateRoot()
            {
                Register<FakeEvent1>(_ => { });
                Register<FakeEvent2>(_ => { });
                Register<BadEvent>(_ => throw new KeyNotFoundException());
            }

            public void DoSomething()
            {
                Apply(new FakeEvent2());
                Apply(new FakeEvent1());
            }

            public void DoSomethingBad()
            {
                Apply(new BadEvent());
            }
        }

        private class FakeEvent1 { }

        private class FakeEvent2 { }

        private class BadEvent { }

        #endregion
    }
}
