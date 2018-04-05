using System;
using System.Threading.Tasks;
using Aggregator.Event;
using Moq;
using NUnit.Framework;

namespace Aggregator.Tests.Event
{
    [TestFixture]
    public class EventDispatcherTests
    {
        private readonly Mock<IEventHandlingScopeFactory> _eventHandlingScopeFactoryMock = new Mock<IEventHandlingScopeFactory>();

        [SetUp]
        public void SetUp()
        {
            _eventHandlingScopeFactoryMock.Reset();
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new EventDispatcher<object>(null));
            Assert.That(ex.ParamName, Is.EqualTo("eventHandlingScopeFactory"));
        }

        [Test]
        public async Task Dispatch_EventArray_ShouldBeginEventHandlingScope()
        {
            var eventHandlingScopeMock = new Mock<IEventHandlingScope<EventA>>();
            _eventHandlingScopeFactoryMock
                .Setup(x => x.BeginScopeFor<EventA>())
                .Returns(eventHandlingScopeMock.Object);

            var dispatcher = new EventDispatcher<object>(_eventHandlingScopeFactoryMock.Object);
            await dispatcher.Dispatch(new[] { new EventA() });

            _eventHandlingScopeFactoryMock.Verify(x => x.BeginScopeFor<EventA>(), Times.Once);
        }

        [Test]
        public async Task Dispatch_EventArray_ShouldResolveAndInvokeHandlers()
        {
            var eventHandlerMockA = new Mock<IEventHandler<EventA>>();
            var eventHandlerMockB = new Mock<IEventHandler<EventB>>();

            var eventHandlingScopeMockA = new Mock<IEventHandlingScope<EventA>>();
            var eventHandlingScopeMockB = new Mock<IEventHandlingScope<EventB>>();
            eventHandlingScopeMockA.Setup(x => x.ResolveHandlers()).Returns(new[] { eventHandlerMockA.Object });
            eventHandlingScopeMockB.Setup(x => x.ResolveHandlers()).Returns(new[] { eventHandlerMockB.Object });

            _eventHandlingScopeFactoryMock.Setup(x => x.BeginScopeFor<EventA>()).Returns(eventHandlingScopeMockA.Object);
            _eventHandlingScopeFactoryMock.Setup(x => x.BeginScopeFor<EventB>()).Returns(eventHandlingScopeMockB.Object);

            var dispatcher = new EventDispatcher<object>(_eventHandlingScopeFactoryMock.Object);
            await dispatcher.Dispatch(new[] { new EventB() });
            eventHandlerMockA.Verify(x => x.Handle(It.IsAny<EventA>()), Times.Never);
            eventHandlerMockB.Verify(x => x.Handle(It.IsAny<EventB>()), Times.Once);

            eventHandlerMockA.Reset();
            eventHandlerMockB.Reset();

            await dispatcher.Dispatch(new object[] { new EventA(), new EventB() });
            eventHandlerMockA.Verify(x => x.Handle(It.IsAny<EventA>()), Times.Once);
            eventHandlerMockB.Verify(x => x.Handle(It.IsAny<EventB>()), Times.Once);
        }

        public class EventA { }

        public class EventB { }
    }
}
