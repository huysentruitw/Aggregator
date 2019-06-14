using System;
using Aggregator.Command;
using Aggregator.Internal;
using FluentAssertions;
using Xunit;

namespace Aggregator.Tests.Command
{
    public class CommandHandlingContextExtensionsTests
    {
        [Fact]
        public void CreateUnitOfWork_ShouldReturnUnitOfWork()
        {
            // Arrange
            var context = new CommandHandlingContext();

            // Act
            var unitOfWork = context.CreateUnitOfWork<string, object>();

            // Assert
            unitOfWork.Should().NotBeNull();
        }

        [Fact]
        public void CreateUnitOfWork_TwiceOnSameContext_ShouldThrowException()
        {
            // Arrange
            var context = new CommandHandlingContext();
            context.CreateUnitOfWork<string, object>();

            // Act / Assert
            Action action = () => context.CreateUnitOfWork<string, object>();
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Unit of work already created*");
        }

        [Fact]
        public void CreateUnitOfWork_ShouldSetCorrectProperty()
        {
            // Arrange
            var context = new CommandHandlingContext();
            var unitOfWork = context.CreateUnitOfWork<string, object>();

            // Act
            var unitOfWorkFromContext = context.Get<UnitOfWork<string, object>>(CommandHandlingContextExtensions.UnitOfWorkKey);

            // Assert
            unitOfWorkFromContext.Should().Be(unitOfWork);
        }

        [Fact]
        public void GetUnitOfWork_ShouldGetCorrectProperty()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<string, object>();
            var context = new CommandHandlingContext();
            context.Set(CommandHandlingContextExtensions.UnitOfWorkKey, unitOfWork);

            // Act
            var unitOfWorkFromContext = context.GetUnitOfWork<string, object>();

            // Assert
            unitOfWorkFromContext.Should().Be(unitOfWork);
        }

        [Fact]
        public void GetUnitOfWork_AfterSetUnitOfWork_ShouldReturnUnitOfWork()
        {
            // Arrange
            var context = new CommandHandlingContext();
            var unitOfWork = context.CreateUnitOfWork<string, object>();

            // Act
            var unitOfWorkFromContext = context.GetUnitOfWork<string, object>();

            // Assert
            unitOfWorkFromContext.Should().Be(unitOfWork);
        }

        [Fact]
        public void GetUnitOfWork_WrongGenericType_ShouldThrowException()
        {
            // Arrange
            var context = new CommandHandlingContext();
            var unitOfWork = context.CreateUnitOfWork<string, EventBase1>();

            // Act / Assert
            Action action = () => context.GetUnitOfWork<string, EventBase2>();
            action.Should().Throw<InvalidCastException>()
                .WithMessage($"Unable to cast object of type '{typeof(UnitOfWork<string, EventBase1>)}' to type '{typeof(UnitOfWork<string, EventBase2>)}'.");
        }

        private class EventBase1 { }

        private class EventBase2 { }
    }
}
