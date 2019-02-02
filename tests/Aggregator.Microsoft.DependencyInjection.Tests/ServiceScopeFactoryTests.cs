using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Aggregator.Microsoft.DependencyInjection.Tests
{
    [TestFixture]
    public class ServiceScopeFactoryTests
    {
        [Test]
        public void Constructor_PassNullAsParentLifetimeScope_ShouldThrowException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ServiceScopeFactory(null));
        }

        [Test]
        public void CreateScope_ShouldCreateAndUseChildScope()
        {
            // Arrange
            var systemParentServiceProviderMock = new Mock<IServiceProvider>();
            var microsoftServiceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            systemParentServiceProviderMock
                .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(microsoftServiceScopeFactoryMock.Object);
            var microsoftChildServiceScopeMock = new Mock<IServiceScope>();
            microsoftServiceScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(microsoftChildServiceScopeMock.Object);
            var systemChildServiceProviderMock = new Mock<IServiceProvider>();
            microsoftChildServiceScopeMock
                .SetupGet(x => x.ServiceProvider)
                .Returns(systemChildServiceProviderMock.Object);
            systemChildServiceProviderMock
                .Setup(x => x.GetService(typeof(int)))
                .Returns(1234);
            var factory = new ServiceScopeFactory(systemParentServiceProviderMock.Object);
            
            // Act
            var scope = factory.CreateScope();

            // Assert
            microsoftServiceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope.GetService(typeof(int)), Is.EqualTo(1234));
        }
    }
}
