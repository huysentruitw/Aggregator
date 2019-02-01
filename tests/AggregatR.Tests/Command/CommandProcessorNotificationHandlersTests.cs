using System;
using AggregatR.Command;
using Moq;
using NUnit.Framework;

namespace AggregatR.Tests.Command
{
    [TestFixture]
    public sealed class CommandProcessorNotificationHandlersTests
    {
        [Test]
        public void OnPrepareContext_PrepareContextIsNull_ShouldNotThrowException()
        {
            // Arrange
            var handlers = new CommandProcessorNotificationHandlers();
            handlers.PrepareContext = null;

            // Act
            Assert.DoesNotThrow(() => handlers.OnPrepareContext(null, null));
        }

        [Test]
        public void OnPrepareContext_PrepareContextIsSet_ShouldCallPrepareContext()
        {
            // Arrange
            var identifier = Guid.NewGuid();
            var context = new CommandHandlingContext();
            var handlers = new CommandProcessorNotificationHandlers();
            var handlerMock = new Mock<Action<object, CommandHandlingContext>>();
            handlers.PrepareContext = handlerMock.Object;
            
            // Act
            handlers.OnPrepareContext(identifier, context);

            // Assert
            handlerMock.Verify(x => x(identifier, context), Times.Once);
        }

        [Test]
        public void OnEnrichEvent_EnrichEventIsNull_ShouldNotThrowException()
        {
            // Arrange
            var handlers = new CommandProcessorNotificationHandlers();
            handlers.EnrichEvent = null;

            // Act
            Assert.DoesNotThrow(() => handlers.OnEnrichEvent(null, null, null));
        }

        [Test]
        public void OnEnrichEvent_EnrichEventIsSet_ShouldCallEnrichEvent()
        {
            // Arrange
            var @event = Guid.NewGuid();
            var command = Guid.NewGuid();
            var handlerResult = Guid.NewGuid();
            var context = new CommandHandlingContext();
            var handlers = new CommandProcessorNotificationHandlers();
            var handlerMock = new Mock<Func<object, object, CommandHandlingContext, object>>();
            handlerMock.Setup(x => x(@event, command, context)).Returns(handlerResult);
            handlers.EnrichEvent = handlerMock.Object;

            // Act
            var result = handlers.OnEnrichEvent(@event, command, context);

            // Assert
            handlerMock.Verify(x => x(@event, command, context), Times.Once);
            Assert.That(result, Is.EqualTo(handlerResult));
        }
    }
}
