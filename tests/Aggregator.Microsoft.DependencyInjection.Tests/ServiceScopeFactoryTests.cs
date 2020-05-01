using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Aggregator.Microsoft.DependencyInjection.Tests
{
    public class ServiceScopeFactoryTests
    {
        [Fact]
        public void Constructor_PassNullAsParentLifetimeScope_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => new ServiceScopeFactory(null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
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
            scope.Should().NotBeNull();
            scope.GetService(typeof(int)).Should().Be(1234);
        }
    }
}
