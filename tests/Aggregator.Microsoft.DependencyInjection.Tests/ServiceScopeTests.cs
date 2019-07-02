using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Aggregator.Microsoft.DependencyInjection.Tests
{
    public class ServiceScopeTests
    {
        [Fact]
        public void Dispose_ShouldDisposeServiceScope()
        {
            // Arrange
            var microsoftServiceScopeMock = new Mock<IServiceScope>();
            var scope = new ServiceScope(microsoftServiceScopeMock.Object);
            microsoftServiceScopeMock.Verify(x => x.Dispose(), Times.Never);

            // Act
            scope.Dispose();

            // Assert
            microsoftServiceScopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void GetService_ShouldResolveServicesFromServiceScope()
        {
            // Arrange
            var service = new DummyService();
            var microsoftServiceScopeMock = new Mock<IServiceScope>();
            var systemServiceProviderMock = new Mock<IServiceProvider>();
            microsoftServiceScopeMock.SetupGet(x => x.ServiceProvider).Returns(systemServiceProviderMock.Object);
            systemServiceProviderMock.Setup(x => x.GetService(typeof(DummyService))).Returns(service);
            var scope = new ServiceScope(microsoftServiceScopeMock.Object);

            // Act
            var result = scope.GetService(typeof(DummyService));

            // Assert
            result.Should().Be(service);
        }

        private sealed class DummyService { }
    }
}
