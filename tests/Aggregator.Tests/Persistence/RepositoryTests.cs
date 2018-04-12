using System;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.Exceptions;
using Aggregator.Internal;
using Aggregator.Persistence;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Persistence
{
    [TestFixture]
    public class RepositoryTests
    {
        private readonly Mock<IEventStore<string, object>> _eventStoreMock = new Mock<IEventStore<string, object>>();
        private readonly CommandHandlingContext _commandHandlingContext = new CommandHandlingContext();

        [SetUp]
        public void SetUp()
        {
            _eventStoreMock.Reset();

            _commandHandlingContext.SetUnitOfWork(new UnitOfWork<string, object>());
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Repository<FakeAggregateRoot>(null, new CommandHandlingContext()));
            Assert.That(ex.ParamName, Is.EqualTo("eventStore"));

            ex = Assert.Throws<ArgumentNullException>(() => new Repository<FakeAggregateRoot>(_eventStoreMock.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("commandHandlingContext"));
        }

        [Test]
        public void Constructor_PassCommandHandlingContextWithoutUnitOfWork_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new Repository<FakeAggregateRoot>(_eventStoreMock.Object, new CommandHandlingContext()));
            Assert.That(ex.ParamName, Is.EqualTo("commandHandlingContext"));
            Assert.That(ex.Message, Does.StartWith("Failed to get unit of work from command handling context"));
        }

        [Test]
        public async Task Contains_UnknownAggregateRoot_ShouldReturnFalse()
        {
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.Contains(unknownIdentifier)).ReturnsAsync(false);
            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            Assert.That(await repository.Contains(unknownIdentifier), Is.False);
        }

        [Test]
        public async Task Contains_KnownAggregateRoot_ShouldReturnTrue()
        {
            var knownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.Contains(knownIdentifier)).ReturnsAsync(true);
            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            Assert.That(await repository.Contains(knownIdentifier), Is.True);
        }

        [Test]
        public async Task Contains_NewAggregateRootAttachedToUnitOfWork_ShouldReturnTrue()
        {
            var newIdentifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            unitOfWork.Attach(new AggregateRootEntity<string, object>(newIdentifier, aggregateRoot, 1));

            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            Assert.That(await repository.Contains(newIdentifier), Is.True);
        }

        [Test]
        public void Get_UnknownAggregateRoot_ShouldThrowException()
        {
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.GetEvents(unknownIdentifier, 1)).ReturnsAsync(Array.Empty<object>());
            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var ex = Assert.ThrowsAsync<AggregateRootNotFoundException<string>>(() => repository.Get(unknownIdentifier));
            Assert.That(ex.Identifier, Is.EqualTo(unknownIdentifier));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{unknownIdentifier}': Aggregate root not found"));
        }

        [Test]
        public async Task Get_KnownAggregateRoot_ShouldReturnInitializedAggregateRoot()
        {
            var knownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.GetEvents(knownIdentifier, 0)).ReturnsAsync(new object[]
            {
                new EventA(),
                new EventB(),
                new EventA()
            });
            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var aggregateRoot = await repository.Get(knownIdentifier);
            Assert.That(aggregateRoot, Is.Not.Null);
            Assert.That(aggregateRoot.EventACount, Is.EqualTo(2));
            Assert.That(aggregateRoot.EventBCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Get_KnownAggregateRoot_ShouldAttachAggregateRootEntityToUnitOfWork()
        {
            var knownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.GetEvents(knownIdentifier, 0)).ReturnsAsync(new object[]
            {
                new EventA(),
                new EventB()
            });
            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var aggregateRootFromRepository = await repository.Get(knownIdentifier);

            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            Assert.That(unitOfWork.TryGet(knownIdentifier, out var aggregateRootEntityFromUnitOfWork), Is.True);
            Assert.That(aggregateRootEntityFromUnitOfWork.Identifier, Is.EqualTo(knownIdentifier));
            Assert.That(aggregateRootEntityFromUnitOfWork.AggregateRoot, Is.EqualTo(aggregateRootFromRepository));
            _eventStoreMock.Verify(x => x.GetEvents(knownIdentifier, 0), Times.Once);
        }

        [Test]
        public async Task Get_AggregateRootAlreadyAttachedToUnitOfWork_ShouldReturnAggregateRootFromUnitOfWork()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            unitOfWork.Attach(new AggregateRootEntity<string, object>(identifier, aggregateRoot, 1));

            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var aggregateRootFromRepository = await repository.Get(identifier);

            Assert.That(aggregateRootFromRepository, Is.EqualTo(aggregateRoot));
            _eventStoreMock.Verify(x => x.GetEvents(identifier, 1), Times.Never);
        }

        [Test]
        public void Add_PassNullAsAggregateRoot_ShouldThrowException()
        {
            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var ex = Assert.ThrowsAsync<ArgumentNullException>(() => repository.Add("some_id", null));
            Assert.That(ex.ParamName, Is.EqualTo("aggregateRoot"));
        }

        [Test]
        public void Add_AggregateRootAlreadyKnownByUnitOfWork_ShouldThrowException()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();

            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            unitOfWork.Attach(new AggregateRootEntity<string, object>(identifier, new FakeAggregateRoot(), 1));

            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var ex = Assert.ThrowsAsync<AggregateRootAlreadyExistsException<string>>(() => repository.Add(identifier, aggregateRoot));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{identifier}': Aggregate root already attached"));
        }

        [Test]
        public void Add_AggregateRootAlreadyKnownByEventStore_ShouldThrowException()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();

            _eventStoreMock
                .Setup(x => x.Contains(identifier))
                .ReturnsAsync(true);

            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var ex = Assert.ThrowsAsync<AggregateRootAlreadyExistsException<string>>(() => repository.Add(identifier, aggregateRoot));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{identifier}': Aggregate root already attached"));
        }

        [Test]
        public async Task Add_NewAggregateRoot_ShouldAttachToUnitOfWork()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();

            var repository = new Repository<FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            await repository.Add(identifier, aggregateRoot);

            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            Assert.That(unitOfWork.TryGet(identifier, out var aggregateRootEntityFromUnitOfWork), Is.True);
            Assert.That(aggregateRootEntityFromUnitOfWork.Identifier, Is.EqualTo(identifier));
            Assert.That(aggregateRootEntityFromUnitOfWork.AggregateRoot, Is.EqualTo(aggregateRoot));
        }

        public class FakeAggregateRoot : AggregateRoot<object>
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
    }
}
