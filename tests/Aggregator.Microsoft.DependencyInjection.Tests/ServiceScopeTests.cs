using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Aggregator.Microsoft.DependencyInjection.Tests
{
    [TestFixture]
    public class ServiceScopeTests
    {
        [Test]
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

        [Test]
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
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(service));
        }

        private sealed class DummyService { }
    }
}
