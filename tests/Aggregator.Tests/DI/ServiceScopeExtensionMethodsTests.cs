using System;
using System.Collections.Generic;
using Aggregator.DI;
using Moq;
using Xunit;

namespace Aggregator.Tests.DI
{
    public sealed class ServiceScopeExtensionMethodsTests
    {
        [Fact]
        public void GetService_ShouldRequestServiceFromServiceScope()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();

            // Act
            serviceScopeMock.Object.GetService<A>();

            // Assert
            serviceScopeMock.Verify(x => x.GetService(typeof(A)), Times.Once);
            serviceScopeMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Once);
        }

        [Fact]
        public void GetServices_ShouldRequestEnumerableListOfServiceFromServiceScope()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();

            // Act
            serviceScopeMock.Object.GetServices<A>();

            // Assert
            serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<A>)), Times.Once);
            serviceScopeMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Once);
        }

        private sealed class A { }
    }
}
