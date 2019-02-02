using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.DI;
using Aggregator.Exceptions;
using Aggregator.Internal;
using Aggregator.Persistence;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Command
{
    [TestFixture]
    public class CommandProcessorTests
    {
        private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        private readonly Mock<IServiceScope> _serviceScopeMock = new Mock<IServiceScope>();
        private readonly Mock<IEventStore<string, IEvent>> _eventStoreMock = new Mock<IEventStore<string, IEvent>>();

        [SetUp]
        public void SetUp()
        {
            _mediatorMock.Reset();
            _serviceScopeFactoryMock.Reset();
            _serviceScopeMock.Reset();
            _eventStoreMock.Reset();

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
            var ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(null, _serviceScopeFactoryMock.Object, _eventStoreMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("mediator"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(_mediatorMock.Object, null, _eventStoreMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("serviceScopeFactory"));

            ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("eventStore"));
        }

        [Test]
        public void Process_PassNullAsCommand_ShouldThrowException()
        {
            // Arrange
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

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
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            _serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        }

        [Test]
        public async Task Process_PassCommand_ShouldResolveContextFromScope()
        {
            // Arrange
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            _serviceScopeMock.Verify(x => x.GetService(typeof(CommandHandlingContext)), Times.Once);
            _serviceScopeMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Once);
        }

        [Test]
        public async Task Process_PassCommand_ShouldForwardCommandToMediator()
        {
            // Arrange
            var command = new FakeCommand();
            var cancellationToken = new CancellationToken();
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(command, cancellationToken);

            // Assert
            _mediatorMock.Verify(x => x.Send(command, cancellationToken), Times.Once);
            _mediatorMock.Verify(x => x.Send(It.IsAny<ICommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Process_PassUnhandledCommand_ShouldThrowException()
        {
            // Arrange
            var mediator = new Mediator(type => null);
            var command = new FakeCommand();
            var processor = new CommandProcessor(mediator, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act / Assert
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
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

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
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<FakeCommand>(), default(CancellationToken)))
                .Callback(() => FakeAggregateAction(x => x.DoSomething()))
                .ReturnsAsync(Unit.Value);
            IEvent[] capturedEvents = null;
            var transactionMock = new Mock<IEventStoreTransaction<string, IEvent>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<IEvent>>(), It.IsAny<CancellationToken>()))
                .Callback<string, long, IEnumerable<IEvent>, CancellationToken>((_, __, events, ___) => capturedEvents = events.ToArray())
                .Returns(Task.CompletedTask);
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

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
        public void Process_StoreEventsThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<FakeCommand>(), default(CancellationToken)))
                .Callback(() => FakeAggregateAction(x => x.DoSomething()))
                .ReturnsAsync(Unit.Value);
            var transactionMock = new Mock<IEventStoreTransaction<string, IEvent>>();
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<IEvent>>(), It.IsAny<CancellationToken>()))
                .Callback(() => throw new InvalidOperationException("StoreEvents failed"));
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act / Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => processor.Process(new FakeCommand()));
            Assert.That(ex.Message, Is.EqualTo("StoreEvents failed"));
            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Once);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Process_MediatorPublishThrowsException_ShouldRollbackTransactionAndRethrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<FakeCommand>(), default(CancellationToken)))
                .Callback(() => FakeAggregateAction(x => x.DoSomething()))
                .ReturnsAsync(Unit.Value);
            _mediatorMock
                .Setup(x => x.Publish(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()))
                .Callback(() => throw new InvalidOperationException("Dispatch failed"));
            var transactionMock = new Mock<IEventStoreTransaction<string, IEvent>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act / Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => processor.Process(new FakeCommand()));
            Assert.That(ex.Message, Is.EqualTo("Dispatch failed"));
            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Once);

            transactionMock.Verify(x => x.Commit(), Times.Never);
            transactionMock.Verify(x => x.Rollback(), Times.Once);
            transactionMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task Process_NoChangedAggregateRootsInUnitOfWork_ShouldNotPublishAnyEvent()
        {
            // Arrange
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<ICommandHandler<FakeCommand>>)))
                .Returns(new[] { new Mock<ICommandHandler<FakeCommand>>().Object });
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Arrange
            _mediatorMock.Verify(x => x.Publish(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Process_ChangedAggregateRootsInUnitOfWork_ShouldPublishEvents()
        {
            // Arrange
            var dispatchedEvents = new List<IEvent>();
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<FakeCommand>(), default(CancellationToken)))
                .Callback(() => FakeAggregateAction(x => x.DoSomething()))
                .ReturnsAsync(Unit.Value);
            _mediatorMock
                .Setup(x => x.Publish(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()))
                .Callback<IEvent, CancellationToken>((@event, _) => dispatchedEvents.Add(@event))
                .Returns(Task.CompletedTask);
            var transactionMock = new Mock<IEventStoreTransaction<string, IEvent>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object);

            // Act
            await processor.Process(new FakeCommand());

            // Assert
            _mediatorMock.Verify(x => x.Publish(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            Assert.That(dispatchedEvents, Is.Not.Null);
            Assert.That(dispatchedEvents, Has.Count.EqualTo(2));
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
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object, notificationHandlers);

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
            var command = new FakeCommand();
            var event1 = new FakeEvent1();
            var event2 = new FakeEvent2();
            IEvent capturedEvent1 = null;
            IEvent capturedEvent2 = null;
            IEnumerable<IEvent> capturedStoredEvents = null;
            var commandHandlingContext = new CommandHandlingContext();
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(CommandHandlingContext)))
                .Returns(commandHandlingContext);
            _mediatorMock
                .Setup(x => x.Send(command, default(CancellationToken)))
                .Callback(() => FakeAggregateAction(x => x.DoSomething()))
                .ReturnsAsync(Unit.Value);
            var transactionMock = new Mock<IEventStoreTransaction<string, IEvent>>();
            _eventStoreMock
                .Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()))
                .Returns(transactionMock.Object);
            transactionMock
                .Setup(x => x.StoreEvents("some_id", 5, It.IsAny<IEnumerable<IEvent>>(), It.IsAny<CancellationToken>()))
                .Callback<string, long, IEnumerable<IEvent>, CancellationToken>((_, __, x, ___) => capturedStoredEvents = x)
                .Returns(Task.CompletedTask);
            var enrichEventMock = new Mock<Func<IEvent, ICommand, CommandHandlingContext, IEvent>>();
            enrichEventMock
                .Setup(x => x(It.Is<IEvent>(y => y is FakeEvent1), command, It.IsAny<CommandHandlingContext>()))
                .Callback<IEvent, ICommand, CommandHandlingContext>((x, _, __) => capturedEvent1 = x)
                .Returns(event1);
            enrichEventMock
                .Setup(x => x(It.Is<IEvent>(y => y is FakeEvent2), command, It.IsAny<CommandHandlingContext>()))
                .Callback<IEvent, ICommand, CommandHandlingContext>((x, _, __) => capturedEvent2 = x)
                .Returns(event2);
            var notificationHandlers = new CommandProcessorNotificationHandlers { EnrichEvent = enrichEventMock.Object };
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object, notificationHandlers);

            // Act
            await processor.Process(command);

            // Assert
            Assert.That(capturedEvent1, Is.Not.Null);
            Assert.That(capturedEvent2, Is.Not.Null);
            Assert.That(capturedStoredEvents, Is.Not.Null);

            enrichEventMock.Verify(x => x(capturedEvent1, command, commandHandlingContext), Times.Once);
            enrichEventMock.Verify(x => x(capturedEvent2, command, commandHandlingContext), Times.Once);

            transactionMock.Verify(x => x.StoreEvents("some_id", 5, capturedStoredEvents, default(CancellationToken)), Times.Once);
            Assert.That(capturedStoredEvents, Does.Contain(event1));
            Assert.That(capturedStoredEvents, Does.Contain(event2));
        }

        [Test]
        public void Process_PrepareContextHandlerThrowsException_ShouldNotBeginTransaction()
        {
            // Arrange
            var handlers = new CommandProcessorNotificationHandlers { PrepareContext = (_, __) => throw new KeyNotFoundException() };
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object, handlers);

            // Act
            Assert.ThrowsAsync<KeyNotFoundException>(() => processor.Process(new FakeCommand()));

            // Assert
            _eventStoreMock.Verify(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>()), Times.Never);
        }

        [Test]
        public void Process_EnrichEventHandlerThrowsException_ShouldRollbackTransaction()
        {
            // Arrange
            var command = new FakeCommand();
            var handlers = new CommandProcessorNotificationHandlers { EnrichEvent = (_, __, ___) => throw new KeyNotFoundException() };
            _mediatorMock
                .Setup(x => x.Send(command, default(CancellationToken)))
                .Callback(() => FakeAggregateAction(x => x.DoSomething()))
                .ReturnsAsync(Unit.Value);
            var transactionMock = new Mock<IEventStoreTransaction<string, IEvent>>();
            _eventStoreMock.Setup(x => x.BeginTransaction(It.IsAny<CommandHandlingContext>())).Returns(transactionMock.Object);
            var processor = new CommandProcessor(_mediatorMock.Object, _serviceScopeFactoryMock.Object, _eventStoreMock.Object, handlers);

            // Act
            Assert.ThrowsAsync<KeyNotFoundException>(() => processor.Process(command));

            // Assert
            transactionMock.Verify(x => x.Rollback(), Times.Once);
            transactionMock.Verify(x => x.Commit(), Times.Never);
        }

        #region Test infrastructure

        public class FakeCommand : ICommand { }

        private class FakeAggregateRoot : AggregateRoot
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

        private class FakeEvent1 : IEvent { }

        private class FakeEvent2 : IEvent { }

        private class BadEvent : IEvent { }

        private void FakeAggregateAction(Action<FakeAggregateRoot> action)
        {
            var context = _serviceScopeMock.Object.GetService<CommandHandlingContext>();
            var unitOfWork = context.GetUnitOfWork<string, IEvent>();
            var aggregateRoot = new FakeAggregateRoot();
            unitOfWork.Attach(new AggregateRootEntity<string, IEvent>("some_id", aggregateRoot, 5));
            action?.Invoke(aggregateRoot);
        }

        #endregion
    }
}
