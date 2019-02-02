using System;
using Aggregator.Command;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Command
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
            var command = new Mock<ICommand>().Object;
            var context = new CommandHandlingContext();
            var handlers = new CommandProcessorNotificationHandlers();
            var handlerMock = new Mock<Action<object, CommandHandlingContext>>();
            handlers.PrepareContext = handlerMock.Object;
            
            // Act
            handlers.OnPrepareContext(command, context);

            // Assert
            handlerMock.Verify(x => x(command, context), Times.Once);
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
            var @event = new Mock<IEvent>().Object;
            var command = new Mock<ICommand>().Object;
            var enrichedEvent = new Mock<IEvent>().Object;
            var context = new CommandHandlingContext();
            var handlers = new CommandProcessorNotificationHandlers();
            var handlerMock = new Mock<Func<IEvent, ICommand, CommandHandlingContext, IEvent>>();
            handlerMock.Setup(x => x(@event, command, context)).Returns(enrichedEvent);
            handlers.EnrichEvent = handlerMock.Object;

            // Act
            var result = handlers.OnEnrichEvent(@event, command, context);

            // Assert
            handlerMock.Verify(x => x(@event, command, context), Times.Once);
            Assert.That(result, Is.EqualTo(enrichedEvent));
        }
    }
}
