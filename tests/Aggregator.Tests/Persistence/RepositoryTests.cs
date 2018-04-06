using System;
using System.Linq;
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
            var ex = Assert.Throws<ArgumentNullException>(() => new Repository<string, object, FakeAggregateRoot>(null, new CommandHandlingContext()));
            Assert.That(ex.ParamName, Is.EqualTo("eventStore"));

            ex = Assert.Throws<ArgumentNullException>(() => new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, null));
            Assert.That(ex.ParamName, Is.EqualTo("commandHandlingContext"));
        }

        [Test]
        public void Constructor_PassCommandHandlingContextWithoutUnitOfWork_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, new CommandHandlingContext()));
            Assert.That(ex.ParamName, Is.EqualTo("commandHandlingContext"));
            Assert.That(ex.Message, Does.StartWith("Failed to get unit of work from command handling context"));
        }

        [Test]
        public async Task Contains_UnknownAggregateRoot_ShouldReturnFalse()
        {
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.Contains(unknownIdentifier)).ReturnsAsync(false);
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            Assert.That(await repository.Contains(unknownIdentifier), Is.False);
        }

        [Test]
        public async Task Contains_KnownAggregateRoot_ShouldReturnTrue()
        {
            var knownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.Contains(knownIdentifier)).ReturnsAsync(true);
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            Assert.That(await repository.Contains(knownIdentifier), Is.True);
        }

        [Test]
        public async Task Contains_NewAggregateRootAttachedToUnitOfWork_ShouldReturnTrue()
        {
            var newIdentifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(newIdentifier, 1);
            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRoot);
            
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            Assert.That(await repository.Contains(newIdentifier), Is.True);
        }

        [Test]
        public async Task Create_ShouldCreateInitializedAggregateRoot()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var aggregateRoot = await repository.Create(identifier);
            Assert.That(aggregateRoot.Identifier, Is.EqualTo(identifier));
        }

        [Test]
        public async Task Create_ShouldAttachNewAggregateRootToUnitOfWork()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);

            Assert.That(unitOfWork.TryGet(identifier, out _), Is.False);
            await repository.Create(identifier);
            Assert.That(unitOfWork.TryGet(identifier, out _), Is.True);
        }

        [Test]
        public void Create_AggregateRootAlreadyAttachedToUnitOfWork_ShouldThrowException()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(identifier, 1);
            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRoot);

            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var ex = Assert.ThrowsAsync<AggregateRootAlreadyAttachedException<string>>(() => repository.Create(identifier));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{identifier}': Aggregate root already attached"));
        }

        [Test]
        public void Get_UnknownAggregateRoot_ShouldThrowException()
        {
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.GetEvents(unknownIdentifier, 1)).ReturnsAsync(Enumerable.Empty<object>());
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var ex = Assert.ThrowsAsync<AggregateRootNotFoundException<string>>(() => repository.Get(unknownIdentifier));
            Assert.That(ex.Identifier, Is.EqualTo(unknownIdentifier));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{unknownIdentifier}': Aggregate root not found"));
        }

        [Test]
        public async Task Get_KnownAggregateRoot_ShouldReturnAggregateRoot()
        {
            var knownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.GetEvents(knownIdentifier, 1)).ReturnsAsync(new object[]
            {
                new EventA(),
                new EventB()
            });
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var aggregateRoot = await repository.Get(knownIdentifier);
            Assert.That(aggregateRoot.Identifier, Is.EqualTo(knownIdentifier));
        }

        [Test]
        public async Task Get_KnownAggregateRoot_ShouldAttachAggregateRootToUnitOfWork()
        {
            var knownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.GetEvents(knownIdentifier, 1)).ReturnsAsync(new object[]
            {
                new EventA(),
                new EventB()
            });
            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var aggregateRootFromRepository = await repository.Get(knownIdentifier);

            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            Assert.That(unitOfWork.TryGet(knownIdentifier, out var aggregateRootFromUnitOfWork), Is.True);
            Assert.That(aggregateRootFromUnitOfWork, Is.EqualTo(aggregateRootFromRepository));
            _eventStoreMock.Verify(x => x.GetEvents(knownIdentifier, 1), Times.Once);
        }

        [Test]
        public async Task Get_AggregateRootAlreadyAttachedToUnitOfWork_ShouldReturnAggregateRootFromUnitOfWork()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new FakeAggregateRoot();
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(identifier, 1);
            var unitOfWork = _commandHandlingContext.GetUnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRoot);

            var repository = new Repository<string, object, FakeAggregateRoot>(_eventStoreMock.Object, _commandHandlingContext);
            var aggregateRootFromRepository = await repository.Get(identifier);

            Assert.That(aggregateRootFromRepository, Is.EqualTo(aggregateRoot));
            _eventStoreMock.Verify(x => x.GetEvents(identifier, 1), Times.Never);
        }

        public class FakeAggregateRoot : AggregateRoot<string, object>
        {
            public FakeAggregateRoot()
            {
                Register<EventA>(_ => { });
                Register<EventB>(_ => { });
            }
        }

        public class EventA { }

        public class EventB { }
    }
}
