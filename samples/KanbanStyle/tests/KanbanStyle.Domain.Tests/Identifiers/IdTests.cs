using System;
using System.Linq;
using Aggregator;
using FluentAssertions;
using KanbanStyle.Domain.Identifiers;
using Xunit;

namespace KanbanStyle.Domain.Tests.Identifiers
{
    public sealed class IdTests
    {
        [Fact]
        public void ImplicitCast_ShouldCastFromGuid()
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act
            Id<MyAggregate> id = guid;

            // Assert
            id.ToString().Should().Be($"MyAggregate.{guid:N}");
        }

        [Fact]
        public void NewId_ShouldConvertToGuid()
        {
            // Arrange
            Id<MyAggregate> id = Id<MyAggregate>.New();

            // Act
            Guid guid = id;

            // Assert
            id.ToString().Should().Be($"MyAggregate.{guid:N}");
        }

        [Fact]
        public void ShouldConvertToString()
        {
            // Arrange
            Id<MyAggregate> id = Id<MyAggregate>.New();
            Guid guid = id;

            // Act
            string value = id;

            // Assert
            value.Should().Be($"MyAggregate.{guid:N}");
        }

        [Fact]
        public void Equals_SameIds_ShouldReturnTrue()
        {
            // Arrange
            Id<MyAggregate> id1 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");
            Id<MyAggregate> id2 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");

            // Act
            var result = id1.Equals(id2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentIds_ShouldReturnFalse()
        {
            // Arrange
            Id<MyAggregate> id1 = Guid.Parse("78254822-1410-4556-b374-719153518a3d");
            Id<MyAggregate> id2 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");

            // Act
            var result = id1.Equals(id2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_SameIds_DifferentAggregateRootTypes_ShouldReturnFalse()
        {
            // Arrange
            Id<MyAggregate> id1 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");
            Id<MyOtherAggregate> id2 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");

            // Act
            var result = id1.Equals(id2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void New_ShouldCreateUniqueId()
        {
            // Act
            Id<MyAggregate>[] ids = Enumerable.Range(0, 10).Select(_ => Id<MyAggregate>.New()).ToArray();

            // Assert
            var uniqueIds = ids.ToHashSet();
            ids.Length.Should().Be(uniqueIds.Count);
        }

        private sealed class MyAggregate : AggregateRoot
        {
        }

        private sealed class MyOtherAggregate : AggregateRoot
        {
        }
    }
}
