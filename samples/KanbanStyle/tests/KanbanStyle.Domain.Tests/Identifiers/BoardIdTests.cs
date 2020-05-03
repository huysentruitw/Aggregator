using System;
using System.Linq;
using FluentAssertions;
using KanbanStyle.Domain.Identifiers;
using Xunit;

namespace KanbanStyle.Domain.Tests.Identifiers
{
    public sealed class BoardIdTests
    {
        [Fact]
        public void ShouldConvertFromGuid()
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act
            BoardId id = guid;

            // Assert
            id.ToString().Should().Be($"Board.{guid:N}");
        }

        [Fact]
        public void ShouldConvertToGuid()
        {
            // Arrange
            BoardId id = BoardId.New();

            // Act
            Guid guid = id;

            // Assert
            id.ToString().Should().Be($"Board.{guid:N}");
        }

        [Fact]
        public void ShouldConvertToString()
        {
            // Arrange
            BoardId id = BoardId.New();
            Guid guid = id;

            // Act
            string value = id;

            // Assert
            value.Should().Be($"Board.{guid:N}");
        }

        [Fact]
        public void Equals_SameIds_ShouldReturnTrue()
        {
            // Arrange
            BoardId id1 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");
            BoardId id2 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");

            // Act
            var result = id1.Equals(id2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentIds_ShouldReturnFalse()
        {
            // Arrange
            BoardId id1 = Guid.Parse("78254822-1410-4556-b374-719153518a3d");
            BoardId id2 = Guid.Parse("b11af48d-bef6-4425-8450-f97d9577254d");

            // Act
            var result = id1.Equals(id2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void New_ShouldCreateUniqueId()
        {
            // Act
            BoardId[] ids = Enumerable.Range(0, 10).Select(_ => BoardId.New()).ToArray();

            // Assert
            var uniqueIds = ids.ToHashSet();
            ids.Length.Should().Be(uniqueIds.Count);
        }
    }
}
