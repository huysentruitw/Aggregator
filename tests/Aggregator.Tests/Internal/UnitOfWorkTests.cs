using System;
using System.Linq;
using Aggregator.Internal;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Internal
{
    [TestFixture]
    public class UnitOfWorkTests
    {
        [Test]
        public void Attach_PassNullAsAggregate_ShouldThrowException()
        {
            var unitOfWork = new UnitOfWork<string, object>();
            var ex = Assert.Throws<ArgumentNullException>(() => unitOfWork.Attach(null));
            Assert.That(ex.ParamName, Is.EqualTo("aggregateRoot"));
        }

        [Test]
        public void Attach_AttachSameIdentifierTwice_ShouldThrowException()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(identifier, 1);

            var unitOfWork = new UnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRoot);
            var ex = Assert.Throws<InvalidOperationException>(() => unitOfWork.Attach(aggregateRoot));
            Assert.That(ex.Message, Is.EqualTo($"Aggregate root with identifier '{identifier}' already attached"));
        }

        [Test]
        public void TryGet_UnknownIdentifier_ShouldReturnFalse()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(identifier, 1);

            var unitOfWork = new UnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRoot);
            Assert.That(unitOfWork.TryGet(Guid.NewGuid().ToString("N"), out var _), Is.False);
        }

        [Test]
        public void TryGet_KnownIdentifier_ShouldReturnTrueAndAggregateRoot()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(identifier, 1);

            var unitOfWork = new UnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRoot);
            Assert.That(unitOfWork.TryGet(identifier, out var resultingAggregateRoot), Is.True);
            Assert.That(resultingAggregateRoot, Is.EqualTo(aggregateRoot));
        }

        [Test]
        public void HasChanges_NoneOfTheTrackedAggregateRootsHaveChanges_ShouldReturnFalse()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(x));
            Assert.That(unitOfWork.HasChanges, Is.False);
        }

        [Test]
        public void HasChanges_OneOfTheTrackedAggregateRootsHasChanges_ShouldReturnTrue()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(x));
            Assert.That(unitOfWork.HasChanges, Is.False);
            aggregateRoots[2].ApplyA();
            Assert.That(unitOfWork.HasChanges, Is.True);
        }

        [Test]
        public void GetChanges_NoneOfTheTrackedAggregateRootsHaveChanges_ShouldReturnEmptyEnumerable()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(x));
            var changes = unitOfWork.GetChanges().ToArray();
            Assert.That(changes, Has.Length.Zero);
        }

        [Test]
        public void GetChanges_SomeOfTheTrackedAggregateRootsHaveChanges_ShouldReturnChangedAggregateRoots()
        {
            var aggregateRoots = CreateAggregateRoots();

            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(x));
            var changes = unitOfWork.GetChanges().ToArray();
            Assert.That(changes, Has.Length.Zero);

            aggregateRoots[2].ApplyA();
            aggregateRoots[3].ApplyA();
            changes = unitOfWork.GetChanges().ToArray();
            Assert.That(changes, Has.Length.EqualTo(2));
        }

        public abstract class FakeAggregateRoot : AggregateRoot<string, object>
        {
            public FakeAggregateRoot()
            {
                Register<EventA>(_ => { });
            }

            public void ApplyA() => Apply(new EventA());
        }

        public class EventA { }

        private FakeAggregateRoot[] CreateAggregateRoots()
            => Enumerable.Range(1, 4)
                .Select(_ =>
                {
                    var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
                    ((IAggregateRootInitializer<string, object>)aggregateRoot).Initialize(Guid.NewGuid().ToString("N"), 1);
                    return aggregateRoot;
                })
                .ToArray();
    }
}
