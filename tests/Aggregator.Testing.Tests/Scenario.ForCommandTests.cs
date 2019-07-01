using System;
using Aggregator.Testing.Tests.TestDomain;
using NUnit.Framework;

namespace Aggregator.Testing.Tests
{
    [TestFixture]
    public sealed partial class ScenarioTests
    {
        [Test]
        public void ForCommand_ShouldReturnCommandContinuation()
        {
            // Act
            var commandContinuation = Scenario.ForCommand(() => Person.Register("Indy Struyck"));

            // Assert
            Assert.That(commandContinuation, Is.Not.Null);
        }

        [Test]
        public void ForCommand_ConstructorIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => Scenario.ForCommand<Person>(null));
            Assert.That(exception.ParamName, Is.EqualTo("constructor"));
        }

        [Test]
        public void ForCommand_ExpectedEvents_Assert_ShouldNotThrowException()
        {
            // Act
            TestDelegate action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(new PersonRegisteredEvent { Name = "Kenny DT"  })
                    .When(person => person.UpdateName("Kenny Di Tunnel"))
                    .Then(new PersonNameUpdatedEvent
                    {
                        Name = new UpdatedInfo<string>("Kenny DT", "Kenny Di Tunnel")
                    })
                    .Assert();

            // Assert
            Assert.DoesNotThrow(action);
        }

        [Test]
        public void ForCommand_ExpectedEventWithWrongContent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected member Name.OldValue to be \"Kenny D\" with a length of 7, but \"Kenny DT\" has a length of 8";

            // Act
            TestDelegate action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(new PersonRegisteredEvent { Name = "Kenny DT" })
                    .When(person => person.UpdateName("Kenny Di Tunnel"))
                    .Then(new PersonNameUpdatedEvent
                    {
                        Name = new UpdatedInfo<string>("Kenny D", "Kenny Di Tunnel")
                    })
                    .Assert();

            // Assert
            var exception = Assert.Throws<AssertionException>(action);
            Assert.That(exception.Message, Does.StartWith(expectedMessage));
        }

        [Test]
        public void ForCommand_DifferentEvent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected type to be Aggregator.Testing.Tests.TestDomain.PersonDeletedEvent, but found Aggregator.Testing.Tests.TestDomain.PersonNameUpdatedEvent";

            // Act
            TestDelegate action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(new PersonRegisteredEvent { Name = "Kenny DT" })
                    .When(person => person.UpdateName("Kenny Di Tunnel"))
                    .Then(new PersonDeletedEvent
                    {
                        Name = "Kenny Di Tunnel"
                    })
                    .Assert();

            // Assert
            var exception = Assert.Throws<AssertionException>(action);
            Assert.That(exception.Message, Does.StartWith(expectedMessage));
        }

        [Test]
        public void ForCommand_UpdateOnDeletedAggregate_AssertOnExpectedThrownException_ShouldSucceed()
        {
            // Act
            TestDelegate action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(
                        new PersonRegisteredEvent { Name = "Jan Itan" },
                        new PersonDeletedEvent { Name = "Jan Itan" })
                    .When(person => person.UpdateName("Jan Kanban"))
                    .Throws(new PersonDeletedException("Jan Itan"))
                    .Assert();

            // Assert
            Assert.DoesNotThrow(action);
        }

        [Test]
        public void ForCommand_UpdateOnDeletedAggregate_AssertOnDifferentThrownException_ShouldThrowException()
        {
            // Act
            TestDelegate action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(
                        new PersonRegisteredEvent { Name = "Jan Itan" },
                        new PersonDeletedEvent { Name = "Jan Itan" })
                    .When(person => person.UpdateName("Jan Kanban"))
                    .Throws(new InvalidOperationException("Jan Itan"))
                    .Assert();

            // Assert
            Assert.Throws<AssertionException>(action);
        }
    }
}
