using System;
using AggregatR.Internal;
using Moq;
using NUnit.Framework;

namespace AggregatR.Tests.Internal
{
    [TestFixture]
    public class AggregateRootEntityTests
    {
        [Test]
        public void Constructor_ShouldSetProperties()
        {
            var identifier = Guid.NewGuid().ToString("N");
            var aggregateRoot = new Mock<AggregateRoot<object>>().Object;
            var expectedVersion = 13;

            var entity = new AggregateRootEntity<string, object>(identifier, aggregateRoot, expectedVersion);
            Assert.That(entity.Identifier, Is.EqualTo(identifier));
            Assert.That(entity.AggregateRoot, Is.EqualTo(aggregateRoot));
            Assert.That(entity.ExpectedVersion, Is.EqualTo(expectedVersion));
        }
    }
}
