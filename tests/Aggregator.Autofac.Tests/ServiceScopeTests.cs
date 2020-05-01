using Autofac;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Autofac.Tests
{
    public class ServiceScopeTests
    {
        [Fact]
        public void Dispose_ShouldDisposeLifetimeScope()
        {
            // Arrange
            var lifetimeScopeMock = new Mock<ILifetimeScope>();
            var scope = new ServiceScope(lifetimeScopeMock.Object);
            lifetimeScopeMock.Verify(x => x.Dispose(), Times.Never);

            // Act
            scope.Dispose();

            // Assert
            lifetimeScopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void GetService_ShouldResolveServicesFromLifetimeScope()
        {
            // Arrange
            var builder = new ContainerBuilder();
            var service = new DummyService();
            builder.RegisterInstance(service);
            var scope = new ServiceScope(builder.Build());

            // Act
            var result = scope.GetService(typeof(DummyService));

            // Assert
            result.Should().Be(service);
        }

        private sealed class DummyService
        {
        }
    }
}
