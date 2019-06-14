using System;
using System.Linq;
using Aggregator.Exceptions;
using Aggregator.Internal;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Tests.Internal
{
    public class UnitOfWorkTests
    {
        [Fact]
        public void Attach_PassNullAsAggregateRootEntity_ShouldThrowException()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<string, object>();

            // Act & Assert
            Action action = () => unitOfWork.Attach(null);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("aggregateRootEntity");
        }

        [Fact]
        public void Attach_AttachSameIdentifierTwice_ShouldThrowException()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var aggregateRootEntity = new AggregateRootEntity<string, object>(identifier, aggregateRoot, 1);
            var unitOfWork = new UnitOfWork<string, object>();

            // Act
            unitOfWork.Attach(aggregateRootEntity);

            // Act & Assert
            Action action = () => unitOfWork.Attach(aggregateRootEntity);
            action.Should().Throw<AggregateRootAlreadyAttachedException<string>>()
                .WithMessage($"Exception for aggregate root with identifier '{identifier}': Aggregate root already attached");
        }

        [Fact]
        public void TryGet_UnknownIdentifier_ShouldReturnFalse()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var aggregateRootEntity = new AggregateRootEntity<string, object>(identifier, aggregateRoot, 1);
            var unitOfWork = new UnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRootEntity);

            // Act
            var result = unitOfWork.TryGet(Guid.NewGuid().ToString("N"), out var _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void TryGet_KnownIdentifier_ShouldReturnTrueAndAggregateRootEntity()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<FakeAggregateRoot>().Object;
            var aggregateRootEntity = new AggregateRootEntity<string, object>(identifier, aggregateRoot, 1);
            var unitOfWork = new UnitOfWork<string, object>();
            unitOfWork.Attach(aggregateRootEntity);

            // Act
            var result = unitOfWork.TryGet(identifier, out var resultingAggregateRootEntity);

            // Assert
            result.Should().BeTrue();
            resultingAggregateRootEntity.Should().Be(aggregateRootEntity);
        }

        [Fact]
        public void HasChanges_NoneOfTheTrackedAggregateRootEntitiesHaveChanges_ShouldReturnFalse()
        {
            // Arrange
            var aggregateRoots = CreateAggregateRoots();
            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, object>(Guid.NewGuid().ToString("N"), x, 0)));

            // Act
            var result = unitOfWork.HasChanges;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasChanges_OneOfTheTrackedAggregateRootsHasChanges_ShouldReturnTrue()
        {
            // Arrange
            var aggregateRoots = CreateAggregateRoots();
            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, object>(Guid.NewGuid().ToString("N"), x, 0)));

            // Act & Assert
            unitOfWork.HasChanges.Should().BeFalse();
            aggregateRoots[2].ApplyA();
            unitOfWork.HasChanges.Should().BeTrue();
        }

        [Fact]
        public void GetChanges_NoneOfTheTrackedAggregateRootsHaveChanges_ShouldReturnEmptyEnumerable()
        {
            // Arrange
            var aggregateRoots = CreateAggregateRoots();
            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, object>(Guid.NewGuid().ToString("N"), x, 0)));

            // Act
            var changes = unitOfWork.GetChanges().ToArray();

            // Assert
            changes.Should().HaveCount(0);
        }

        [Fact]
        public void GetChanges_SomeOfTheTrackedAggregateRootsHaveChanges_ShouldReturnChangedAggregateRoots()
        {
            // Arrange
            var aggregateRoots = CreateAggregateRoots();
            var unitOfWork = new UnitOfWork<string, object>();
            Array.ForEach(aggregateRoots, x => unitOfWork.Attach(new AggregateRootEntity<string, object>(Guid.NewGuid().ToString("N"), x, 0)));

            // Act & Assert
            var changes = unitOfWork.GetChanges().ToArray();
            changes.Should().HaveCount(0);

            aggregateRoots[2].ApplyA();
            aggregateRoots[3].ApplyA();
            changes = unitOfWork.GetChanges().ToArray();
            changes.Should().HaveCount(2);
        }

        public abstract class FakeAggregateRoot : AggregateRoot
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
                .Select(_ => new Mock<FakeAggregateRoot>().Object)
                .ToArray();
    }
}
