using System;
using Autofac;
using NUnit.Framework;

namespace Aggregator.Autofac.Tests
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
            var parentScope = new ContainerBuilder().Build();
            ILifetimeScope capturedChildScope = null;
            parentScope.ChildLifetimeScopeBeginning += (_, e) => capturedChildScope = e.LifetimeScope;
            var factory = new ServiceScopeFactory(parentScope);

            // Act
            var scope = factory.CreateScope();

            // Assert
            Assert.That(scope, Is.Not.Null);
            Assert.That(capturedChildScope, Is.Not.Null);
            Assert.That(scope.GetService(typeof(ILifetimeScope)), Is.EqualTo(capturedChildScope));
        }
    }
}
