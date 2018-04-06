using System;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Exceptions;
using Aggregator.Persistence;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Persistence
{
    [TestFixture]
    public class RepositoryTests
    {
        private readonly Mock<IEventStore<string, object>> _eventStoreMock = new Mock<IEventStore<string, object>>();

        [SetUp]
        public void SetUp()
        {
            _eventStoreMock.Reset();
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Repository<string, object, FakeAggregate>(null));
            Assert.That(ex.ParamName, Is.EqualTo("eventStore"));
        }

        [Test]
        public async Task Contains_UnknownAggregateRoot_ShouldReturnFalse()
        {
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.Contains(unknownIdentifier)).ReturnsAsync(false);
            var repository = new Repository<string, object, FakeAggregate>(_eventStoreMock.Object);
            Assert.That(await repository.Contains(unknownIdentifier), Is.False);
        }

        [Test]
        public async Task Contains_KnownAggregateRoot_ShouldReturnTrue()
        {
            var knownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.Contains(knownIdentifier)).ReturnsAsync(true);
            var repository = new Repository<string, object, FakeAggregate>(_eventStoreMock.Object);
            Assert.That(await repository.Contains(knownIdentifier), Is.True);
        }

        [Test]
        public async Task Create_ShouldCreateInitializedAggregateRoot()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var repository = new Repository<string, object, FakeAggregate>(_eventStoreMock.Object);
            var aggregateRoot = await repository.Create(identifier);
            Assert.That(aggregateRoot.Identifier, Is.EqualTo(identifier));
        }

        [Test]
        public void Get_UnknownAggregateRoot_ShouldThrowException()
        {
            var unknownIdentifier = Guid.NewGuid().ToString("N");
            _eventStoreMock.Setup(x => x.GetEvents(unknownIdentifier, 1)).ReturnsAsync(Enumerable.Empty<object>());
            var repository = new Repository<string, object, FakeAggregate>(_eventStoreMock.Object);
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
            var repository = new Repository<string, object, FakeAggregate>(_eventStoreMock.Object);
            var aggregateRoot = await repository.Get(knownIdentifier);
            Assert.That(aggregateRoot.Identifier, Is.EqualTo(knownIdentifier));
        }

        public class FakeAggregate : AggregateRoot<string, object>
        {
            public FakeAggregate()
            {
                Register<EventA>(_ => { });
                Register<EventB>(_ => { });
            }
        }

        public class EventA { }

        public class EventB { }
    }
}
