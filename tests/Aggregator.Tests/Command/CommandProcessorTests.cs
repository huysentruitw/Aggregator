using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.DI;
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
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        private readonly Mock<IServiceScope> _serviceScopeMock = new Mock<IServiceScope>();
        private readonly Mock<IEventDispatcher<object>> _eventDispatcherMock = new Mock<IEventDispatcher<object>>();
        private readonly Mock<IEventStore<string, object>> _eventStoreMock = new Mock<IEventStore<string, object>>();

        [SetUp]
        public void SetUp()
        {
            _eventDispatcherMock.Reset();
            _eventStoreMock.Reset();
            _serviceScopeMock.Reset();
            _serviceScopeFactoryMock.Reset();

            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(new CommandHandlingContext());
            _serviceScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(_serviceScopeMock.Object);
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            // Act / Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(null, _eventDispatcherMock.Object, _eventStoreMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("serviceScopeFactory"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(_serviceScopeFactoryMock.Object, null, _eventStoreMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("eventDispatcher"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("eventStore"));
        }

        [Test]
        public void Process_PassNullAsCommand_ShouldThrowException()
        {
            // Arrange
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act / Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(() => processor.Process(null));
            Assert.That(ex.ParamName, Is.EqualTo("command"));
        }

        [Test]
        public async Task Process_PassCommand_ShouldCreateServiceScope()
        {
            // Arrange
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            _serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        }

        [Test]
        public async Task Process_PassCommand_ShouldResolveContextAndHandlersFromScope()
        {
            // Arrange
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            _serviceScopeMock.Verify(x => x.GetService(typeof(CommandHandlingContext)), Times.Once);
            _serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)), Times.Once);
            _serviceScopeMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Exactly(2));
        }

        [Test]
        public async Task Process_PassCommand_ShouldForwardCommandToHandlers()
        {
            // Arrange
            var handlerMocks = new[]
            {
                new Mock<ICommandHandler<FakeCommand>>(),
                new Mock<ICommandHandler<FakeCommand>>()
            };
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(handlerMocks.Select(x => x.Object));
            var command = new FakeCommand();
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(command);

            // Assert
            handlerMocks[0].Verify(x => x.Handle(command), Times.Once);
            handlerMocks[1].Verify(x => x.Handle(command), Times.Once);
        }

        [Test]
        public void Process_PassUnhandledCommand_ShouldThrowException()
        {
            var command = new FakeCommand();
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);
            var ex = Assert.ThrowsAsync<UnhandledCommandException>(() => processor.Process(command));
            Assert.That(ex.Command, Is.EqualTo(command));
            Assert.That(ex.CommandType, Is.EqualTo(typeof(FakeCommand)));
            Assert.That(ex.Message, Is.EqualTo("Unhandled command 'FakeCommand'"));
        }

        [Test]
        public async Task Process_NoChangedAggregateRootsInUnitOfWork_ShouldNotBeginEventStoreTransaction()
        {
            // Arrange
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Never);
        }

        [Test]
        public async Task Process_ChangedAggregateRootsInUnitOfWork_ShouldCommitChangesToEventStore()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            object[] capturedEvents = null;
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>()))
                .Callback<string, long, IEnumerable<object>>((_, __, events) => capturedEvents = events.ToArray())
                .Returns(Task.CompletedTask);
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
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
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomethingBad()) });
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act / Assert
            Assert.ThrowsAsync<KeyNotFoundException>(() => processor.Process(new FakeCommand()));

            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Never);
        }

        [Test]
        public void Process_StoreEventsThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>()))
                .Callback(() => throw new InvalidOperationException("StoreEvents failed"));
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act / Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => processor.Process(new FakeCommand()));
            Assert.That(ex.Message, Is.EqualTo("StoreEvents failed"));
            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Once);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Process_DispatchThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            _eventDispatcherMock
                .Setup(x => x.Dispatch(It.IsAny<object[]>()))
                .Callback(() => throw new InvalidOperationException("Dispatch failed"));
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act / Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => processor.Process(new FakeCommand()));
            Assert.That(ex.Message, Is.EqualTo("Dispatch failed"));
            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Once);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task Process_NoChangedAggregateRootsInUnitOfWork_ShouldDispatchEmptyEventsArray()
        {
            // Arrange
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Arrange
            _eventDispatcherMock.Verify(x => x.Dispatch(Array.Empty<object>()), Times.Once);
        }

        [Test]
        public async Task Process_ChangedAggregateRootsInUnitOfWork_ShouldDispatchEvents()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            object[] dispatchedEvents = null;
            _eventDispatcherMock
                .Setup(x => x.Dispatch(It.IsAny<object[]>()))
                .Callback<object[]>(events => dispatchedEvents = events)
                .Returns(Task.CompletedTask);
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            _eventDispatcherMock.Verify(x => x.Dispatch(It.IsAny<object[]>()), Times.Once);
            Assert.That(dispatchedEvents, Is.Not.Null);
            Assert.That(dispatchedEvents, Has.Length.EqualTo(2));
            Assert.That(dispatchedEvents[0], Is.InstanceOf<FakeEvent2>());
            Assert.That(dispatchedEvents[1], Is.InstanceOf<FakeEvent1>());
        }

        [Test]
        public async Task Process_WithPrepareContextNotificationHandler_ShouldCallPrepareContextNotificationHandler()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var command = new FakeCommand();
            var prepareContextMock = new Mock<Action<object, CommandHandlingContext>>();
            var notificationHandlers = new CommandProcessorNotificationHandlers { PrepareContext = prepareContextMock.Object };
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object, notificationHandlers);

            // Act
            await processor.Process(command);

            // Assert
            prepareContextMock.Verify(x => x(command, commandHandlingContext), Times.Once);
            prepareContextMock.Verify(x => x(It.IsAny<object>(), It.IsAny<CommandHandlingContext>()), Times.Once);
        }

        [Test]
        public async Task Process_WithEnrichEventNotificationHandler_ShouldCallEnrichEventNotificationHandler()
        {
            // Arrange
            object capturedEvent1 = null;
            object capturedEvent2 = null;
            IEnumerable<object> capturedStoredEvents = null;
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>()))
                .Callback<string, long, IEnumerable<object>>((_, __, x) => capturedStoredEvents = x)
                .Returns(Task.CompletedTask);
            var command = new FakeCommand();
            var event1 = new FakeEvent1();
            var event2 = new FakeEvent2();
            var enrichEventMock = new Mock<Func<object, object, CommandHandlingContext, object>>();
            enrichEventMock
                .Setup(x => x(It.Is<object>(y => y is FakeEvent1), command, It.IsAny<CommandHandlingContext>()))
                .Callback<object, object, CommandHandlingContext>((x, _, __) => capturedEvent1 = x)
                .Returns(event1);
            enrichEventMock
                .Setup(x => x(It.Is<object>(y => y is FakeEvent2), command, It.IsAny<CommandHandlingContext>()))
                .Callback<object, object, CommandHandlingContext>((x, _, __) => capturedEvent2 = x)
                .Returns(event2);
            var notificationHandlers = new CommandProcessorNotificationHandlers { EnrichEvent = enrichEventMock.Object };
            var processor = new CommandProcessor(_serviceScopeFactoryMock.Object, _eventDispatcherMock.Object, _eventStoreMock.Object, notificationHandlers);

            // Act
            await processor.Process(command);

            // Assert
            Assert.That(capturedEvent1, Is.Not.Null);
            Assert.That(capturedEvent2, Is.Not.Null);
            Assert.That(capturedStoredEvents, Is.Not.Null);

            enrichEventMock.Verify(x => x(capturedEvent1, command, commandHandlingContext), Times.Once);
            enrichEventMock.Verify(x => x(capturedEvent2, command, commandHandlingContext), Times.Once);

            transactionMock.Verify(x => x.StoreEvents("some_id", 5, capturedStoredEvents), Times.Once);
            Assert.That(capturedStoredEvents, Does.Contain(event1));
            Assert.That(capturedStoredEvents, Does.Contain(event2));
        }

        #region Test infrastructure

        public class FakeCommand { }

        private class FakeCommandHandler : ICommandHandler<FakeCommand>
        {
            private readonly CommandHandlingContext _context;
            private readonly Action<FakeAggregateRoot> _action;

            public FakeCommandHandler(CommandHandlingContext context, Action<FakeAggregateRoot> action)
            {
                _context = context;
                _action = action;
            }

            public Task Handle(FakeCommand command)
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
