using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.DI;
using Aggregator.Event;
using Aggregator.Exceptions;
using Aggregator.Internal;
using Aggregator.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Tests.Command
{
    public class CommandProcessorTests
    {
        private Mock<IServiceScopeFactory> NewServiceScopeFactoryMock => new Mock<IServiceScopeFactory>();

        private Mock<IEventStore<string, object>> NewEventStoreMock => new Mock<IEventStore<string, object>>();

        private Mock<IEventDispatcher<object>> NewEventDispatcherMock => new Mock<IEventDispatcher<object>>();

        [Fact]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            // Act / Assert
            Action action = () => new CommandProcessor(null, NewEventStoreMock.Object, NewEventDispatcherMock.Object);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("serviceScopeFactory");

            action = () => new CommandProcessor(NewServiceScopeFactoryMock.Object, null, NewEventDispatcherMock.Object);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("eventStore");

            action = () => new CommandProcessor(NewServiceScopeFactoryMock.Object, NewEventStoreMock.Object, null);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("eventDispatcher");
        }

        [Fact]
        public void Process_PassNullAsCommand_ShouldThrowException()
        {
            // Arrange
            var processor = new CommandProcessor(NewServiceScopeFactoryMock.Object, NewEventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act / Assert
            Func<Task> action = () => processor.Process(null);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("command");
        }

        [Fact]
        public async Task Process_PassCommand_ShouldCreateServiceScope()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            serviceScopeMock.Setup(x => x.GetService(typeof(CommandHandlingContext))).Returns(new CommandHandlingContext());
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, NewEventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        }

        [Fact]
        public async Task Process_PassCommand_ShouldResolveContextAndHandlersFromScope()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            serviceScopeMock.Setup(x => x.GetService(typeof(CommandHandlingContext))).Returns(new CommandHandlingContext());
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, NewEventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            serviceScopeMock.Verify(x => x.GetService(typeof(CommandHandlingContext)), Times.Once);
            serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)), Times.Once);
            serviceScopeMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Process_PassCommand_ShouldForwardCommandToHandlers()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            serviceScopeMock.Setup(x => x.GetService(typeof(CommandHandlingContext))).Returns(new CommandHandlingContext());
            var handlerMocks = new[]
            {
                new Mock<ICommandHandler<FakeCommand>>(),
                new Mock<ICommandHandler<FakeCommand>>()
            };
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(handlerMocks.Select(x => x.Object));
            var command = new FakeCommand();
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, NewEventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act
            await processor.Process(command);

            // Assert
            handlerMocks[0].Verify(x => x.Handle(command, default(CancellationToken)), Times.Once);
            handlerMocks[1].Verify(x => x.Handle(command, default(CancellationToken)), Times.Once);
        }

        [Fact]
        public void Process_PassUnhandledCommand_ShouldThrowException()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            serviceScopeMock.Setup(x => x.GetService(typeof(CommandHandlingContext))).Returns(new CommandHandlingContext());
            var command = new FakeCommand();
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, NewEventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act & Assert
            Func<Task> action = () => processor.Process(command);
            var ex = action.Should().Throw<UnhandledCommandException>()
                .WithMessage("Unhandled command 'FakeCommand'")
                .Which;
            ex.Command.Should().Be(command);
            ex.CommandType.Should().Be(typeof(FakeCommand));
        }

        [Fact]
        public async Task Process_NoChangedAggregateRootsInUnitOfWork_ShouldNotBeginEventStoreTransaction()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            serviceScopeMock.Setup(x => x.GetService(typeof(CommandHandlingContext))).Returns(new CommandHandlingContext());
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var eventStoreMock = NewEventStoreMock;
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, eventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Never);
        }

        [Fact]
        public async Task Process_ChangedAggregateRootsInUnitOfWork_ShouldCommitChangesToEventStore()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            var commandHandlingContext = new CommandHandlingContext();
            serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            object[] capturedEvents = null;
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>(), default(CancellationToken)))
#pragma warning disable IDE1006 // Naming Styles
                .Callback<string, long, IEnumerable<object>, CancellationToken>((_, __, events, ___) => capturedEvents = events.ToArray())
#pragma warning restore IDE1006 // Naming Styles
                .Returns(Task.CompletedTask);
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, eventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);
            capturedEvents.Should().NotBeNull();
            capturedEvents.Should().HaveCount(2);
            capturedEvents[0].Should().BeOfType<FakeEvent2>();
            capturedEvents[1].Should().BeOfType<FakeEvent1>();

            transactionMock.Verify(x => x.Commit(), Times.Once);
            transactionMock.Verify(x => x.Rollback(), Times.Never);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Process_ApplyingEventToAggregateThrowsException_ShouldNotBeginTransaction()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            var commandHandlingContext = new CommandHandlingContext();
            serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomethingBad()) });
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, eventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act / Assert
            Func<Task> action = () => processor.Process(new FakeCommand());
            action.Should().Throw<KeyNotFoundException>();
            eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Never);
        }

        [Fact]
        public void Process_StoreEventsThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            var commandHandlingContext = new CommandHandlingContext();
            serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>(), default(CancellationToken)))
                .Callback(() => throw new InvalidOperationException("StoreEvents failed"));
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, eventStoreMock.Object, NewEventDispatcherMock.Object);

            // Act / Assert
            Func<Task> action = () => processor.Process(new FakeCommand());
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("StoreEvents failed");
            eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Never);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Process_DispatchThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            var commandHandlingContext = new CommandHandlingContext();
            serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            var eventDispatcherMock = NewEventDispatcherMock;
            eventDispatcherMock
                .Setup(x => x.Dispatch(It.IsAny<object[]>(), default(CancellationToken)))
                .Callback(() => throw new InvalidOperationException("Dispatch failed"));
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, eventStoreMock.Object, eventDispatcherMock.Object);

            // Act / Assert
            Func<Task> action = () => processor.Process(new FakeCommand());
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Dispatch failed");
            eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Never);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task Process_NoChangedAggregateRootsInUnitOfWork_ShouldNotDispatchAnyEvent()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            serviceScopeMock.Setup(x => x.GetService(typeof(CommandHandlingContext))).Returns(new CommandHandlingContext());
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var eventDispatcherMock = NewEventDispatcherMock;
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, NewEventStoreMock.Object, eventDispatcherMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Arrange
            eventDispatcherMock.Verify(x => x.Dispatch(It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Process_ChangedAggregateRootsInUnitOfWork_ShouldDispatchEvents()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            var commandHandlingContext = new CommandHandlingContext();
            serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            object[] dispatchedEvents = null;
            var eventDispatcherMock = NewEventDispatcherMock;
            eventDispatcherMock
                .Setup(x => x.Dispatch(It.IsAny<object[]>(), default(CancellationToken)))
                .Callback<object[], CancellationToken>((events, _) => dispatchedEvents = events)
                .Returns(Task.CompletedTask);
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, eventStoreMock.Object, eventDispatcherMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            eventDispatcherMock.Verify(x => x.Dispatch(It.IsAny<object[]>(), default(CancellationToken)), Times.Once);
            dispatchedEvents.Should().NotBeNull();
            dispatchedEvents.Should().HaveCount(2);
            dispatchedEvents[0].Should().BeOfType<FakeEvent2>();
            dispatchedEvents[1].Should().BeOfType<FakeEvent1>();
        }

        [Fact]
        public async Task Process_WithPrepareContextNotificationHandler_ShouldCallPrepareContextNotificationHandler()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            var commandHandlingContext = new CommandHandlingContext();
            serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var command = new FakeCommand();
            var prepareContextMock = new Mock<Action<object, CommandHandlingContext>>();
            var notificationHandlers = new CommandProcessorNotificationHandlers { PrepareContext = prepareContextMock.Object };
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, NewEventStoreMock.Object, NewEventDispatcherMock.Object, notificationHandlers);

            // Act
            await processor.Process(command);

            // Assert
            prepareContextMock.Verify(x => x(command, commandHandlingContext), Times.Once);
            prepareContextMock.Verify(x => x(It.IsAny<object>(), It.IsAny<CommandHandlingContext>()), Times.Once);
        }

#pragma warning disable IDE1006 // Naming Styles
        [Fact]
        public async Task Process_WithEnrichEventNotificationHandler_ShouldCallEnrichEventNotificationHandler()
        {
            // Arrange
            object capturedEvent1 = null;
            object capturedEvent2 = null;
            IEnumerable<object> capturedStoredEvents = null;
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = NewServiceScopeFactoryMock;
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(() => serviceScopeMock.Object);
            var commandHandlingContext = new CommandHandlingContext();
            serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new FakeCommandHandler(commandHandlingContext, x => x.DoSomething()) });
            var transactionMock = new Mock<IEventStoreTransaction<string, object>>();
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<object>>(), default(CancellationToken)))
                .Callback<string, long, IEnumerable<object>, CancellationToken>((_, __, x, ___) => capturedStoredEvents = x)
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
            var processor = new CommandProcessor(serviceScopeFactoryMock.Object, eventStoreMock.Object, NewEventDispatcherMock.Object, notificationHandlers);

            // Act
            await processor.Process(command);

            // Assert
            capturedEvent1.Should().NotBeNull();
            capturedEvent2.Should().NotBeNull();
            capturedStoredEvents.Should().NotBeNull();

            enrichEventMock.Verify(x => x(capturedEvent1, command, commandHandlingContext), Times.Once);
            enrichEventMock.Verify(x => x(capturedEvent2, command, commandHandlingContext), Times.Once);

            transactionMock.Verify(x => x.StoreEvents("some_id", 5, capturedStoredEvents, default(CancellationToken)), Times.Once);
            capturedStoredEvents.Should().Contain(event1);
            capturedStoredEvents.Should().Contain(event2);
        }
#pragma warning restore IDE1006 // Naming Styles

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

            public Task Handle(FakeCommand command, CancellationToken cancellationToken)
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
