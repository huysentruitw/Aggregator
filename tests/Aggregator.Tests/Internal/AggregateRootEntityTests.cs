using System;
using Aggregator.Internal;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Tests.Internal
{
    public class AggregateRootEntityTests
    {
        [Fact]
        public void Constructor_ShouldSetProperties()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<AggregateRoot<object>>().Object;
            var expectedVersion = 13;

            // Act
            var entity = new AggregateRootEntity<string, object>(identifier, aggregateRoot, expectedVersion);

            // Assert
            entity.Identifier.Should().Be(identifier);
            entity.AggregateRoot.Should().Be(aggregateRoot);
            entity.ExpectedVersion.Should().Be(expectedVersion);
        }
    }
}
