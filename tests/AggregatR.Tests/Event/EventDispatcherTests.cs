using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AggregatR.DI;
using AggregatR.Event;
using Moq;
using NUnit.Framework;

namespace AggregatR.Tests.Event
{
    [TestFixture]
    public class EventDispatcherTests
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        private readonly Mock<IServiceScope> _serviceScopeMock = new Mock<IServiceScope>();
        private readonly Mock<IEventHandler<EventA>> _eventAHandlerMock = new Mock<IEventHandler<EventA>>();
        private readonly Mock<IEventHandler<EventB>> _eventBHandlerMock = new Mock<IEventHandler<EventB>>();

        [SetUp]
        public void SetUp()
        {
            _serviceScopeFactoryMock.Reset();
            _serviceScopeMock.Reset();
            _eventAHandlerMock.Reset();
            _eventBHandlerMock.Reset();

            _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<IEventHandler<EventA>>)))
                .Returns(new[] { _eventAHandlerMock.Object });
            _serviceScopeMock
                .Setup(x => x.GetService(typeof(IEnumerable<IEventHandler<EventB>>)))
                .Returns(new[] { _eventBHandlerMock.Object });
        }

        [Test]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            // Act / Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new EventDispatcher<object>(null));
            Assert.That(ex.ParamName, Is.EqualTo("serviceScopeFactory"));
        }

        [Test]
        public void Dispatch_PassNullAsOrEmptyEventArray_ShouldNotThrowException()
        {
            // Arrange
            var dispatcher = new EventDispatcher<object>(_serviceScopeFactoryMock.Object);

            // Act / Assert
            Assert.DoesNotThrowAsync(() => dispatcher.Dispatch(null));
            Assert.DoesNotThrowAsync(() => dispatcher.Dispatch(Array.Empty<object>()));
        }

        [Test]
        public async Task Dispatch_EventArray_ShouldCreateServiceScope()
        {
            // Arrange
            var serviceScopeMock = new Mock<IServiceScope>();
            _serviceScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(serviceScopeMock.Object);
            var dispatcher = new EventDispatcher<object>(_serviceScopeFactoryMock.Object);

            // Act
            await dispatcher.Dispatch(new[] { new EventA() });

            // Assert
            _serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        }

        [Test]
        public async Task Dispatch_EventArray_ShouldDisposeServiceScope()
        {
            // Arrange
            var eventHandlingScopeMock = new Mock<IServiceScope>();
            _serviceScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(eventHandlingScopeMock.Object);
            var dispatcher = new EventDispatcher<object>(_serviceScopeFactoryMock.Object);

            // Act
            await dispatcher.Dispatch(new[] { new EventA() });

            // Assert
            eventHandlingScopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task Dispatch_SingleEvent_ShouldResolveAndInvokeSingleHandler()
        {
            // Arrange
            var singleEvent = new EventB();
            var dispatcher = new EventDispatcher<object>(_serviceScopeFactoryMock.Object);

            // Act
            await dispatcher.Dispatch(new[] { singleEvent });

            // Assert
            _serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<IEventHandler<EventA>>)), Times.Never);
            _serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<IEventHandler<EventB>>)), Times.Once);
            _eventAHandlerMock.Verify(x => x.Handle(It.IsAny<EventA>()), Times.Never);
            _eventBHandlerMock.Verify(x => x.Handle(It.IsAny<EventB>()), Times.Once);
            _eventBHandlerMock.Verify(x => x.Handle(singleEvent), Times.Once);
        }

        [Test]
        public async Task Dispatch_MultipleEvents_ShouldResolveAndInvokeHandlers()
        {
            // Arrange
            var eventA = new EventA();
            var eventB = new EventB();
            var dispatcher = new EventDispatcher<object>(_serviceScopeFactoryMock.Object);

            // Act
            await dispatcher.Dispatch(new object[] { eventB, eventA });

            // Assert
            _serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<IEventHandler<EventA>>)), Times.Once);
            _serviceScopeMock.Verify(x => x.GetService(typeof(IEnumerable<IEventHandler<EventB>>)), Times.Once);
            _eventAHandlerMock.Verify(x => x.Handle(It.IsAny<EventA>()), Times.Once);
            _eventAHandlerMock.Verify(x => x.Handle(eventA), Times.Once);
            _eventBHandlerMock.Verify(x => x.Handle(It.IsAny<EventB>()), Times.Once);
            _eventBHandlerMock.Verify(x => x.Handle(eventB), Times.Once);
        }

        public class EventA { }

        public class EventB { }
    }
}
