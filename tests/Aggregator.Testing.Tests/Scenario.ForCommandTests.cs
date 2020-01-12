using System;
using Aggregator.Testing.Tests.TestDomain;
using FluentAssertions;
using Xunit;

namespace Aggregator.Testing.Tests
{
    public sealed partial class ScenarioTests
    {
        [Fact]
        public void ForCommand_ShouldReturnCommandContinuation()
        {
            // Act
            var commandContinuation = Scenario.ForCommand(() => Person.Register("Indy Struyck"));

            // Assert
            commandContinuation.Should().NotBeNull();
        }

        [Fact]
        public void ForCommand_ConstructorIsNull_ShouldThrowArgumentNullException()
        {
            // Act
            Action action = () => Scenario.ForCommand<Person>(null);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("constructor");
        }

        [Fact]
        public void ForCommand_ExpectedEvents_Assert_ShouldNotThrowException()
        {
            // Act
            Action action = () =>
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
            action.Should().NotThrow();
        }

        [Fact]
        public void ForCommand_ExpectedEventWithWrongContent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected event:*\"Kenny D\"*but got event:*\"Kenny DT\"*";

            // Act
            Action action = () =>
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
            action.Should().Throw<AggregatorTestingException>()
                .WithMessage(expectedMessage);
        }

        [Fact]
        public void ForCommand_DifferentEvent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected event at index 0 to be of type Aggregator.Testing.Tests.TestDomain.PersonDeletedEvent, but got an event of type Aggregator.Testing.Tests.TestDomain.PersonNameUpdatedEvent instead";

            // Act
            Action action = () =>
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
            action.Should().Throw<AggregatorTestingException>()
                .WithMessage(expectedMessage);
        }

        [Fact]
        public void ForCommand_UpdateOnDeletedAggregate_AssertOnExpectedThrownException_ShouldSucceed()
        {
            // Act
            Action action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(
                        new PersonRegisteredEvent { Name = "Jan Itan" },
                        new PersonDeletedEvent { Name = "Jan Itan" })
                    .When(person => person.UpdateName("Jan Kanban"))
                    .Throws(new PersonDeletedException("Jan Itan"))
                    .Assert();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void ForCommand_UpdateOnDeletedAggregate_AssertOnExpectedThrowExceptionButWithDifferentPropertyValues_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected exception:*Other name*to be thrown, but got exception:*Jan Itan*";

            // Act
            Action action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(
                        new PersonRegisteredEvent { Name = "Jan Itan" },
                        new PersonDeletedEvent { Name = "Jan Itan" })
                    .When(person => person.UpdateName("Jan Kanban"))
                    .Throws(new PersonDeletedException("Other name"))
                    .Assert();

            // Assert
            action.Should().Throw<AggregatorTestingException>()
                .WithMessage(expectedMessage);
        }

        [Fact]
        public void ForCommand_UpdateOnDeletedAggregate_AssertOnDifferentThrownException_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected an exception of type System.InvalidOperationException to be thrown, but got an exception of type Aggregator.Testing.Tests.TestDomain.PersonDeletedException instead";

            // Act
            Action action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .Given(
                        new PersonRegisteredEvent { Name = "Jan Itan" },
                        new PersonDeletedEvent { Name = "Jan Itan" })
                    .When(person => person.UpdateName("Jan Kanban"))
                    .Throws(new InvalidOperationException("Jan Itan"))
                    .Assert();

            // Assert
            action.Should().Throw<AggregatorTestingException>()
                .WithMessage(expectedMessage);
        }

        [Fact]
        public void ForCommand_ExpectExceptionToBeThrownButNothingIsBeingThrown_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected an exception of type System.InvalidOperationException to be thrown, but no exception was thrown instead";

            // Act
            Action action = () =>
                Scenario
                    .ForCommand(Person.Factory)
                    .When(person => person.UpdateName("Piet Huysentruyt"))
                    .Throws<InvalidOperationException>()
                    .Assert();

            // Assert
            action.Should().Throw<AggregatorTestingException>()
                .WithMessage(expectedMessage);
        }
    }
}
