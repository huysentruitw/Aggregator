using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aggregator.DI;
using Aggregator.Event;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aggregator.Tests.Event
{
    public class EventDispatcherTests
    {
        [Fact]
        public void Constructor_PassInvalidArguments_ShouldThrowException()
        {
            // Act / Assert
            Action action = () => new EventDispatcher<object>(null);
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("serviceScopeFactory");
        }

        [Fact]
        public void Dispatch_PassNullAsOrEmptyEventArray_ShouldNotThrowException()
        {
            // Arrange
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            var dispatcher = new EventDispatcher<object>(serviceScopeFactoryMock.Object);

            // Act / Assert
            Func<Task> action = () => dispatcher.Dispatch(null, default);
            action.Should().NotThrow();

            action = () => dispatcher.Dispatch(Array.Empty<object>(), default);
            action.Should().NotThrow();
        }

        [Fact]
        public async Task Dispatch_EventArray_ShouldCreateServiceScope()
        {
            // Arrange
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(serviceScopeMock.Object);
            var dispatcher = new EventDispatcher<object>(serviceScopeFactoryMock.Object);

            // Act
            await dispatcher.Dispatch(new[] { new EventA() }, default);

            // Assert
            serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        }

        [Fact]
        public async Task Dispatch_EventArray_ShouldDisposeServiceScope()
        {
            // Arrange
            var eventHandlingScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(eventHandlingScopeMock.Object);
            var dispatcher = new EventDispatcher<object>(serviceScopeFactoryMock.Object);

            // Act
            await dispatcher.Dispatch(new[] { new EventA() }, default);

            // Assert
            eventHandlingScopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task Dispatch_SingleEvent_ShouldInvokeCorrectHandler()
        {
            // Arrange
            var singleEvent = new EventB();
            var fakeServiceScopeFactory = new FakeServiceScopeFactory();
            var dispatcher = new EventDispatcher<object>(fakeServiceScopeFactory);

            // Act
            await dispatcher.Dispatch(new[] { singleEvent }, default);

            // Assert
            fakeServiceScopeFactory.EventAHandlerMock.Verify(x => x.Handle(It.IsAny<EventA>(), default), Times.Never);
            fakeServiceScopeFactory.EventBHandlerMock.Verify(x => x.Handle(It.IsAny<EventB>(), default), Times.Once);
            fakeServiceScopeFactory.EventBHandlerMock.Verify(x => x.Handle(singleEvent, default), Times.Once);
        }

        [Fact]
        public async Task Dispatch_MultipleEvents_ShouldInvokeCorrectHandlers()
        {
            // Arrange
            var eventA = new EventA();
            var eventB = new EventB();
            var fakeServiceScopeFactory = new FakeServiceScopeFactory();
            var dispatcher = new EventDispatcher<object>(fakeServiceScopeFactory);

            // Act
            await dispatcher.Dispatch(new object[] { eventB, eventA }, default);

            // Assert
            fakeServiceScopeFactory.EventAHandlerMock.Verify(x => x.Handle(It.IsAny<EventA>(), default), Times.Once);
            fakeServiceScopeFactory.EventAHandlerMock.Verify(x => x.Handle(eventA, default), Times.Once);
            fakeServiceScopeFactory.EventBHandlerMock.Verify(x => x.Handle(It.IsAny<EventB>(), default), Times.Once);
            fakeServiceScopeFactory.EventBHandlerMock.Verify(x => x.Handle(eventB, default), Times.Once);
        }

        public class FakeServiceScopeFactory : IServiceScopeFactory
        {
            public IServiceScope CreateScope() => new FakeServiceScope(this);

            public Mock<IEventHandler<EventA>> EventAHandlerMock { get; } = new Mock<IEventHandler<EventA>>();

            public Mock<IEventHandler<EventB>> EventBHandlerMock { get; } = new Mock<IEventHandler<EventB>>();

            private class FakeServiceScope : IServiceScope
            {
                private readonly FakeServiceScopeFactory _factory;

                public FakeServiceScope(FakeServiceScopeFactory factory)
                {
                    _factory = factory;
                }

                public void Dispose()
                {
                }

                public object GetService(Type serviceType)
                {
                    if (serviceType == typeof(IEnumerable<IEventHandler<EventA>>))
                        return new[] { _factory.EventAHandlerMock.Object };
                    if (serviceType == typeof(IEnumerable<IEventHandler<EventB>>))
                        return new[] { _factory.EventBHandlerMock.Object };
                    return null;
                }
            }
        }

        public class EventA
        {
        }

        public class EventB
        {
        }
    }
}
