using System;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.Exceptions;
using Aggregator.Internal;
using Aggregator.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Tests.Persistence
{
    public class RepositoryTests
    {

        [Fact]
        public void Constructor_PassInvalidEventStoreArgument_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => new Repository<FakeAggregateRoot>(null, new CommandHandlingContext());
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("eventStore");
        }

        [Fact]
        public void Constructor_PassInvalidCommandHandlingContextArgument_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => new Repository<FakeAggregateRoot>(NewEventStoreMock.Object, null);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("commandHandlingContext");
        }

        [Fact]
        public void Constructor_PassCommandHandlingContextWithoutUnitOfWork_ShouldThrowException()
        {
            // Act / Assert
            Action action = () => new Repository<FakeAggregateRoot>(NewEventStoreMock.Object, new CommandHandlingContext());
            action.Should().Throw<ArgumentException>()
                .WithMessage("Failed to get unit of work from command handling context*")
                .Which.ParamName.Should().Be("commandHandlingContext");
        }

        [Fact]
        public async Task Contains_UnknownAggregateRoot_ShouldReturnFalse()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            commandHandlingContext.CreateUnitOfWork<string, object>();
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock.Setup(x => x.Contains(unknownIdentifier)).ReturnsAsync(false);
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act
            var result = await repository.Contains(unknownIdentifier);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Contains_KnownAggregateRoot_ShouldReturnTrue()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            commandHandlingContext.CreateUnitOfWork<string, object>();
            var knownIdentifier = Guid.NewGuid().ToString("N");
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock.Setup(x => x.Contains(knownIdentifier)).ReturnsAsync(true);
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act
            var result = await repository.Contains(knownIdentifier);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Contains_NewAggregateRootAttachedToUnitOfWork_ShouldReturnTrue()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            commandHandlingContext.CreateUnitOfWork<string, object>();
            var newIdentifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            var unitOfWork = commandHandlingContext.GetUnitOfWork<string, object>();
            unitOfWork.Attach(new AggregateRootEntity<string, object>(newIdentifier, aggregateRoot, 1));
            var repository = new Repository<FakeAggregateRoot>(NewEventStoreMock.Object, commandHandlingContext);

            // Act
            var result = await repository.Contains(newIdentifier);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Get_UnknownAggregateRoot_ShouldThrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            commandHandlingContext.CreateUnitOfWork<string, object>();
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock.Setup(x => x.GetEvents(unknownIdentifier, 1)).ReturnsAsync(Array.Empty<object>());
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act / Assert
            Func<Task> action = () => repository.Get(unknownIdentifier);
            action.Should().Throw<AggregateRootNotFoundException<string>>()
                .WithMessage($"Exception for aggregate root with identifier '{unknownIdentifier}': Aggregate root not found")
                .Which.Identifier.Should().Be(unknownIdentifier);

            // Arrange
            eventStoreMock.Setup(x => x.GetEvents(unknownIdentifier, 1)).ReturnsAsync((object[])null);

            // Act / Assert
            action.Should().Throw<AggregateRootNotFoundException<string>>()
                .WithMessage($"Exception for aggregate root with identifier '{unknownIdentifier}': Aggregate root not found")
                .Which.Identifier.Should().Be(unknownIdentifier);
        }

        [Fact]
        public async Task Get_KnownAggregateRoot_ShouldReturnInitializedAggregateRoot()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            commandHandlingContext.CreateUnitOfWork<string, object>();
            var knownIdentifier = Guid.NewGuid().ToString("N");
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock.Setup(x => x.GetEvents(knownIdentifier, 0)).ReturnsAsync(new object[]
            {
                new EventA(),
                new EventB(),
                new EventA()
            });
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act
            var aggregateRoot = await repository.Get(knownIdentifier);

            // Assert
            aggregateRoot.Should().NotBeNull();
            aggregateRoot.EventACount.Should().Be(2);
            aggregateRoot.EventBCount.Should().Be(1);
        }

        [Fact]
        public async Task Get_KnownAggregateRoot_ShouldAttachAggregateRootEntityToUnitOfWork()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            var unitOfWork = commandHandlingContext.CreateUnitOfWork<string, object>();
            var knownIdentifier = Guid.NewGuid().ToString("N");
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock.Setup(x => x.GetEvents(knownIdentifier, 0)).ReturnsAsync(new object[]
            {
                new EventA(),
                new EventB()
            });
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act
            var aggregateRootFromRepository = await repository.Get(knownIdentifier);

            // Assert
            unitOfWork.TryGet(knownIdentifier, out var aggregateRootEntityFromUnitOfWork).Should().BeTrue();
            aggregateRootEntityFromUnitOfWork.Identifier.Should().Be(knownIdentifier);
            aggregateRootEntityFromUnitOfWork.AggregateRoot.Should().Be(aggregateRootFromRepository);
            eventStoreMock.Verify(x => x.GetEvents(knownIdentifier, 0), Times.Once);
        }

        [Fact]
        public async Task Get_AggregateRootAlreadyAttachedToUnitOfWork_ShouldReturnAggregateRootFromUnitOfWork()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            var unitOfWork = commandHandlingContext.CreateUnitOfWork<string, object>(); var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            unitOfWork.Attach(new AggregateRootEntity<string, object>(identifier, aggregateRoot, 1));
            var eventStoreMock = NewEventStoreMock;
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act
            var aggregateRootFromRepository = await repository.Get(identifier);

            // Assert
            aggregateRootFromRepository.Should().Be(aggregateRoot);
            eventStoreMock.Verify(x => x.GetEvents(identifier, 1), Times.Never);
        }

        [Fact]
        public void Add_PassNullAsAggregateRoot_ShouldThrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            commandHandlingContext.CreateUnitOfWork<string, object>();
            var eventStoreMock = NewEventStoreMock;
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act / Assert
            Func<Task> action = () => repository.Add("some_id", null);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("aggregateRoot");
        }

        [Fact]
        public void Add_AggregateRootAlreadyKnownByUnitOfWork_ShouldThrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            var unitOfWork = commandHandlingContext.CreateUnitOfWork<string, object>();
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            unitOfWork.Attach(new AggregateRootEntity<string, object>(identifier, new FakeAggregateRoot(), 1));
            var repository = new Repository<FakeAggregateRoot>(NewEventStoreMock.Object, commandHandlingContext);

            // Act / Assert
            Func<Task> action = () => repository.Add(identifier, aggregateRoot);
            action.Should().Throw<AggregateRootAlreadyExistsException<string>>()
                .WithMessage($"Exception for aggregate root with identifier '{identifier}': Aggregate root already attached");
        }

        [Fact]
        public void Add_AggregateRootAlreadyKnownByEventStore_ShouldThrowException()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            commandHandlingContext.CreateUnitOfWork<string, object>();
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            var eventStoreMock = NewEventStoreMock;
            eventStoreMock.Setup(x => x.Contains(identifier)).ReturnsAsync(true);
            var repository = new Repository<FakeAggregateRoot>(eventStoreMock.Object, commandHandlingContext);

            // Act / Assert
            Func<Task> action = () => repository.Add(identifier, aggregateRoot);
            action.Should().Throw<AggregateRootAlreadyExistsException<string>>()
                .WithMessage($"Exception for aggregate root with identifier '{identifier}': Aggregate root already attached");
        }

        [Fact]
        public async Task Add_NewAggregateRoot_ShouldAttachToUnitOfWork()
        {
            // Arrange
            var commandHandlingContext = new CommandHandlingContext();
            var unitOfWork = commandHandlingContext.CreateUnitOfWork<string, object>();
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            var repository = new Repository<FakeAggregateRoot>(NewEventStoreMock.Object, commandHandlingContext);

            // Act
            await repository.Add(identifier, aggregateRoot);

            // Assert
            unitOfWork.TryGet(identifier, out var aggregateRootEntityFromUnitOfWork).Should().BeTrue();
            aggregateRootEntityFromUnitOfWork.Identifier.Should().Be(identifier);
            aggregateRootEntityFromUnitOfWork.AggregateRoot.Should().Be(aggregateRoot);
        }

        public class FakeAggregateRoot : AggregateRoot
        {
            public FakeAggregateRoot()
            {
                Register<EventA>(_ => EventACount++);
                Register<EventB>(_ => EventBCount++);
            }

            public int EventACount { get; private set; } = 0;
            public int EventBCount { get; private set; } = 0;
        }

        public class EventA { }

        public class EventB { }

        private Mock<IEventStore<string, object>> NewEventStoreMock => new Mock<IEventStore<string, object>>();
    }
}
