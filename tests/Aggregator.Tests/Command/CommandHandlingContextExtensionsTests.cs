using System;
using Aggregator.Command;
using Aggregator.Internal;
using NUnit.Framework;

namespace Aggregator.Tests.Command
{
    [TestFixture]
    public class CommandHandlingContextExtensionsTests
    {
        [Test]
        public void CreateUnitOfWork_ShouldReturnUnitOfWork()
        {
            // Arrange
            var context = new CommandHandlingContext();

            // Act
            var unitOfWork = context.CreateUnitOfWork<string, IEvent>();

            // Assert
            Assert.That(unitOfWork, Is.Not.Null);
        }

        [Test]
        public void CreateUnitOfWork_TwiceOnSameContext_ShouldThrowException()
        {
            // Arrange
            var context = new CommandHandlingContext();
            context.CreateUnitOfWork<string, IEvent>();

            // Act / Assert
            var ex = Assert.Throws<InvalidOperationException>(() => context.CreateUnitOfWork<string, IEvent>());
            Assert.That(ex.Message, Does.StartWith("Unit of work already created"));
        }

        [Test]
        public void CreateUnitOfWork_ShouldSetCorrectProperty()
        {
            // Arrange
            var context = new CommandHandlingContext();
            var unitOfWork = context.CreateUnitOfWork<string, IEvent>();

            // Act
            var unitOfWorkFromContext = context.Get<UnitOfWork<string, IEvent>>(CommandHandlingContextExtensions.UnitOfWorkKey);

            // Assert
            Assert.That(unitOfWorkFromContext, Is.EqualTo(unitOfWork));
        }

        [Test]
        public void GetUnitOfWork_ShouldGetCorrectProperty()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<string, IEvent>();
            var context = new CommandHandlingContext();
            context.Set(CommandHandlingContextExtensions.UnitOfWorkKey, unitOfWork);

            // Act
            var unitOfWorkFromContext = context.GetUnitOfWork<string, IEvent>();

            // Assert
            Assert.That(unitOfWorkFromContext, Is.EqualTo(unitOfWork));
        }

        [Test]
        public void GetUnitOfWork_AfterSetUnitOfWork_ShouldReturnUnitOfWork()
        {
            // Arrange
            var context = new CommandHandlingContext();
            var unitOfWork = context.CreateUnitOfWork<string, IEvent>();

            // Act
            var unitOfWorkFromContext = context.GetUnitOfWork<string, IEvent>();

            // Assert
            Assert.That(unitOfWorkFromContext, Is.EqualTo(unitOfWork));
        }

        [Test]
        public void GetUnitOfWork_WrongGenericType_ShouldThrowException()
        {
            // Arrange
            var context = new CommandHandlingContext();
            var unitOfWork = context.CreateUnitOfWork<string, EventBase1>();

            // Act / Assert
            var ex = Assert.Throws<InvalidCastException>(() => context.GetUnitOfWork<string, EventBase2>());
            Assert.That(ex.Message, Is.EqualTo($"Unable to cast object of type '{typeof(UnitOfWork<string, EventBase1>)}' to type '{typeof(UnitOfWork<string, EventBase2>)}'."));
        }

        private class EventBase1 : IEvent { }

        private class EventBase2 : IEvent { }
    }
}
