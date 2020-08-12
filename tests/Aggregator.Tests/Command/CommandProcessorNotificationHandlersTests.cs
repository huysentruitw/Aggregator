using System;
using Aggregator.Command;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Tests.Command
{
    public sealed class CommandProcessorNotificationHandlersTests
    {
        [Fact]
        public void OnPrepareContext_PrepareContextIsNull_ShouldNotThrowException()
        {
            // Arrange
            var handlers = new CommandProcessorNotificationHandlers
            {
                PrepareContext = null,
            };

            // Act & Assert
            Action action = () => handlers.OnPrepareContext(null, null);
            action.Should().NotThrow();
        }

        [Fact]
        public void OnPrepareContext_PrepareContextIsSet_ShouldCallPrepareContext()
        {
            // Arrange
            var command = new Mock<object>().Object;
            var context = new CommandHandlingContext();
            var handlers = new CommandProcessorNotificationHandlers();
            var handlerMock = new Mock<Action<object, CommandHandlingContext>>();
            handlers.PrepareContext = handlerMock.Object;

            // Act
            handlers.OnPrepareContext(command, context);

            // Assert
            handlerMock.Verify(x => x(command, context), Times.Once);
        }

        [Fact]
        public void OnEnrichEvent_EnrichEventIsNull_ShouldNotThrowException()
        {
            // Arrange
            var handlers = new CommandProcessorNotificationHandlers
            {
                EnrichEvent = null,
            };

            // Act & Assert
            Action action = () => handlers.OnEnrichEvent(null, null, null);
            action.Should().NotThrow();
        }

        [Fact]
        public void OnEnrichEvent_EnrichEventIsSet_ShouldCallEnrichEvent()
        {
            // Arrange
            var @event = new Mock<object>().Object;
            var command = new Mock<object>().Object;
            var enrichedEvent = new Mock<object>().Object;
            var context = new CommandHandlingContext();
            var handlers = new CommandProcessorNotificationHandlers();
            var handlerMock = new Mock<Func<object, object, CommandHandlingContext, object>>();
            handlerMock.Setup(x => x(@event, command, context)).Returns(enrichedEvent);
            handlers.EnrichEvent = handlerMock.Object;

            // Act
            var result = handlers.OnEnrichEvent(@event, command, context);

            // Assert
            handlerMock.Verify(x => x(@event, command, context), Times.Once);
            result.Should().Be(enrichedEvent);
        }
    }
}
