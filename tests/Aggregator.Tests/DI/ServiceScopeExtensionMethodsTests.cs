using System;
using System.Collections.Generic;
using Aggregator.DI;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.DI
{
    [TestFixture]
    public sealed class ServiceScopeExtensionMethodsTests
    {
        [Test]
        public void GetService_ShouldRequestServiceFromServiceScope()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();

            // Act
            ServiceScopeExtensionMethods.GetService<A>(serviceScopeMock.Object);

            // Assert
            serviceScopeMock.Verify(x => x.GetService(typeof(A)), Times.Once);
            serviceScopeMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Once);
        }

        [Test]
        public void GetServices_ShouldRequestEnumerableListOfServiceFromServiceScope()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();

            // Act
            ServiceScopeExtensionMethods.GetServices<A>(serviceScopeMock.Object);

            // Assert
            serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<A>)), Times.Once);
            serviceScopeMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Once);
        }

        private sealed class A { }
    }
}
