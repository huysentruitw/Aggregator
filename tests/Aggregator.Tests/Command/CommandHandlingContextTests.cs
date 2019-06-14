using System;
using Aggregator.Command;
using FluentAssertions;
using Xunit;

namespace Aggregator.Tests.Command
{
    public sealed class CommandHandlingContextTests
    {
        [Fact]
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
            result.Should().Be(value);
        }

        [Fact]
        public void Get_UnknownKey_ShouldReturnDefaultValue()
        {
            // Arrange
            var context = new CommandHandlingContext();

            // Act / Assert
            context.Get<Test>("test").Should().BeNull();
            context.Get<int>("test").Should().Be(0);
        }

        [Fact]
        public void Get_TypeMismatch_ShouldThrowException()
        {
            // Arrange
            var key = $"{Guid.NewGuid():N}";
            var value = new Test();
            var context = new CommandHandlingContext();
            context.Set(key, value);

            // Act / Assert
            Action action = () => context.Get<int>(key);
            action.Should().Throw<InvalidCastException>();
        }

        private sealed class Test { }
    }
}
