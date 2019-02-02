using System;
using System.Linq;
using Aggregator.Exceptions;
using Aggregator.Internal;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Internal
{
    [TestFixture]
    public class UnitOfWorkTests
    {
        [Test]
        public void Attach_PassNullAsAggregateRootEntity_ShouldThrowException()
        {
            var unitOfWork = new UnitOfWork<string, IEvent>();
            var ex = Assert.Throws<ArgumentNullException>(() => unitOfWork.Attach(null));
            Assert.That(ex.ParamName, Is.EqualTo("aggregateRootEntity"));
        }

        [Test]
        public void Attach_AttachSameIdentifierTwice_ShouldThrowException()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var aggregateRootEntity = new AggregateRootEntity<string, IEvent>(identifier, aggregateRoot, 1);

            var unitOfWork = new UnitOfWork<string, IEvent>();
            unitOfWork.Attach(aggregateRootEntity);
            var ex = Assert.Throws<AggregateRootAlreadyAttachedException<string>>(() => unitOfWork.Attach(aggregateRootEntity));
            Assert.That(ex.Message, Is.EqualTo($"Exception for aggregate root with identifier '{identifier}': Aggregate root already attached"));
        }

        [Test]
        public void TryGet_UnknownIdentifier_ShouldReturnFalse()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var aggregateRootEntity = new AggregateRootEntity<string, IEvent>(identifier, aggregateRoot, 1);

            var unitOfWork = new UnitOfWork<string, IEvent>();
            unitOfWork.Attach(aggregateRootEntity);
            Assert.That(unitOfWork.TryGet(Guid.NewGuid().ToString("N"), out var _), Is.False);
        }

        [Test]
        public void TryGet_KnownIdentifier_ShouldReturnTrueAndAggregateRootEntity()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var aggregateRootEntity = new AggregateRootEntity<string, IEvent>(identifier, aggregateRoot, 1);

            var unitOfWork = new UnitOfWork<string, IEvent>();
            unitOfWork.Attach(aggregateRootEntity);
            Assert.That(unitOfWork.TryGet(identifier, out var resultingAggregateRootEntity), Is.True);
            Assert.That(resultingAggregateRootEntity, Is.EqualTo(aggregateRootEntity));
        }

        [Test]
        public void HasChanges_NoneOfTheTrackedAggregateRootEntitiesHaveChanges_ShouldReturnFalse()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, IEvent>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, IEvent>(Guid.NewGuid().ToString("N"), x, 0)));
            Assert.That(unitOfWork.HasChanges, Is.False);
        }

        [Test]
        public void HasChanges_OneOfTheTrackedAggregateRootsHasChanges_ShouldReturnTrue()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, IEvent>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, IEvent>(Guid.NewGuid().ToString("N"), x, 0)));
            Assert.That(unitOfWork.HasChanges, Is.False);
            aggregateRoots[2].ApplyA();
            Assert.That(unitOfWork.HasChanges, Is.True);
        }

        [Test]
        public void GetChanges_NoneOfTheTrackedAggregateRootsHaveChanges_ShouldReturnEmptyEnumerable()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, IEvent>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, IEvent>(Guid.NewGuid().ToString("N"), x, 0)));
            var changes = unitOfWork.GetChanges().ToArray();
            Assert.That(changes, Has.Length.Zero);
        }

        [Test]
        public void GetChanges_SomeOfTheTrackedAggregateRootsHaveChanges_ShouldReturnChangedAggregateRoots()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, IEvent>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, IEvent>(Guid.NewGuid().ToString("N"), x, 0)));
            var changes = unitOfWork.GetChanges().ToArray();
            Assert.That(changes, Has.Length.Zero);

            aggregateRoots[2].ApplyA();
            aggregateRoots[3].ApplyA();
            changes = unitOfWork.GetChanges().ToArray();
            Assert.That(changes, Has.Length.EqualTo(2));
        }

        public abstract class FakeAggregateRoot : AggregateRoot
        {
            public FakeAggregateRoot()
            {
                Register<EventA>(_ => { });
            }

            public void ApplyA() => Apply(new EventA());
        }

        public class EventA : IEvent { }

        private FakeAggregateRoot[] CreateAggregateRoots()
            => Enumerable.Range(1, 4)
                .Select(_ => new Mock<FakeAggregateRoot>().Object)
                .ToArray();
    }
}
