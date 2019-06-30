using System;
using Autofac;
using FluentAssertions;
using Xunit;

namespace Aggregator.Autofac.Tests
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
            var parentScope = new ContainerBuilder().Build();
            ILifetimeScope capturedChildScope = null;
            parentScope.ChildLifetimeScopeBeginning += (_, e) => capturedChildScope = e.LifetimeScope;
            var factory = new ServiceScopeFactory(parentScope);

            // Act
            var scope = factory.CreateScope();

            // Assert
            scope.Should().NotBeNull();
            capturedChildScope.Should().NotBeNull();
            scope.GetService(typeof(ILifetimeScope)).Should().Be(capturedChildScope);
        }
    }
}
