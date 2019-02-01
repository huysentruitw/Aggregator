using System.Linq;
using AggregatR.DI;
using NUnit.Framework;

namespace AggregatR.Tests.DI
{
    [TestFixture]
    public sealed class ReflectionTypeLocatorTests
    {
        [Test]
        public void Locate_CommandHandlers()
        {
            // Act
            var types = ReflectionTypeLocator
                .Locate(typeof(IFakeCommandHandler<>), typeof(FakeCommand).Assembly)
                .ToArray();

            // Assert
            Assert.That(types, Has.Length.EqualTo(2));
            Assert.That(types, Does.Contain(typeof(FakeCommandHandlerA)));
            Assert.That(types, Does.Contain(typeof(FakeCommandHandlerB)));
        }
    }

    public sealed class FakeCommand { }

    public sealed class FakeCommandHandlerA : IFakeCommandHandler<FakeCommand>
    {
    }

    public sealed class FakeCommandHandlerB : IFakeCommandHandler<FakeCommand>
    {
    }

    public interface IFakeCommandHandler<TCommand> { }
}
