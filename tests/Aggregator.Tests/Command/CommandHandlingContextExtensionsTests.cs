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
        public void SetUnitOfWork_ShouldSetCorrectProperty()
        {
            var unitOfWork = new UnitOfWork<string, object>();

            var context = new CommandHandlingContext();
            context.SetUnitOfWork(unitOfWork);

            var unitOfWorkFromContext = context.Get<UnitOfWork<string, object>>(CommandHandlingContextExtensions.UnitOfWorkKey);
            Assert.That(unitOfWorkFromContext, Is.EqualTo(unitOfWork));
        }

        [Test]
        public void GetUnitOfWork_ShouldGetCorrectProperty()
        {
            var unitOfWork = new UnitOfWork<string, object>();

            var context = new CommandHandlingContext();
            context.Set(CommandHandlingContextExtensions.UnitOfWorkKey, unitOfWork);

            var unitOfWorkFromContext = context.GetUnitOfWork<string, object>();
            Assert.That(unitOfWorkFromContext, Is.EqualTo(unitOfWork));
        }

        [Test]
        public void GetUnitOfWork_AfterSetUnitOfWork_ShouldReturnUnitOfWork()
        {
            var unitOfWork = new UnitOfWork<string, object>();

            var context = new CommandHandlingContext();
            context.SetUnitOfWork(unitOfWork);

            var unitOfWorkFromContext = context.GetUnitOfWork<string, object>();
            Assert.That(unitOfWorkFromContext, Is.EqualTo(unitOfWork));
        }

        [Test]
        public void GetUnitOfWork_WrongGenericType_ShouldThrowException()
        {
            var unitOfWork = new UnitOfWork<string, int>();

            var context = new CommandHandlingContext();
            context.SetUnitOfWork(unitOfWork);

            var ex = Assert.Throws<InvalidCastException>(() => context.GetUnitOfWork<string, string>());
            Assert.That(ex.Message, Is.EqualTo($"Unable to cast object of type '{typeof(UnitOfWork<string, int>)}' to type '{typeof(UnitOfWork<string, string>)}'."));
        }
    }
}
