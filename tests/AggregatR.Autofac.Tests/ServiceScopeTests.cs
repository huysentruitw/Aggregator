using Autofac;
using Moq;
using NUnit.Framework;

namespace AggregatR.Autofac.Tests
{
    [TestFixture]
    public class ServiceScopeTests
    {
        [Test]
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

        [Test]
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
            Assert.That(result, Is.EqualTo(service));
        }

        private sealed class DummyService { }
    }
}
