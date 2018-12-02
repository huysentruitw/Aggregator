using System;
using Aggregator.Command;
using NUnit.Framework;

namespace Aggregator.Tests.Command
{
    [TestFixture]
    public sealed class CommandHandlingContextTests
    {
        [Test]
        public void SetGet_ShouldReturnValue()
        {
            // Arrange
            var key = $"{Guid.NewGuid():N}";
            var value = new Test();
            var context = new CommandHandlingContext();

            // Act
            context.Set(key, value);
            var result = context.Get<Test>(key);

            // Assert
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        public void Get_UnknownKey_ShouldReturnDefaultValue()
        {
            // Arrange
            var context = new CommandHandlingContext();

            // Act / Assert
            Assert.That(context.Get<Test>("test"), Is.Null);
            Assert.That(context.Get<int>("test"), Is.Zero);
        }

        [Test]
        public void Get_TypeMismatch_ShouldThrowException()
        {
            // Arrange
            var key = $"{Guid.NewGuid():N}";
            var value = new Test();
            var context = new CommandHandlingContext();

            // Act
            context.Set(key, value);

            // Act / Assert
            Assert.Throws<InvalidCastException>(() => context.Get<int>(key));
        }

        private sealed class Test { }
    }
}
