using System;
using System.Text;
using Aggregator.Testing.Tests.TestDomain;
using FluentAssertions;
using Xunit;

namespace Aggregator.Testing.Tests
{
    public sealed partial class ScenarioTests
    {
        [Fact]
        public void ForConstructor_ShouldReturnConstructorContinuation()
        {
            // Act
            var constructorContinuation = Scenario.ForConstructor(() => Person.Register("Indy Struyck"));

            // Assert
            constructorContinuation.Should().NotBeNull();
        }

        [Fact]
        public void ForConstructor_ConstructorIsNull_ShouldThrowArgumentNullException()
        {
            // Act
            Action action = () => Scenario.ForConstructor<Person>(null);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("constructor");
        }

        [Fact]
        public void ForConstructor_ExpectedEvent_Assert_ShouldNotThrowException()
        {
            // Act
            Action action = () =>
                Scenario
                    .ForConstructor(() => Person.Register("Kenny Di Tunnel"))
                    .Then(new PersonRegisteredEvent
                    {
                        Name = "Kenny Di Tunnel"
                    })
                    .Assert();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void ForConstructor_ExpectedEventWithWrongContent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected event:*\"Marieke Rechte\"*but got event:*\"Kenny Di Tunnel\"*";

            // Act
            Action action = () =>
                Scenario
                    .ForConstructor(() => Person.Register("Kenny Di Tunnel"))
                    .Then(new PersonRegisteredEvent
                    {
                        Name = "Marieke Rechte"
                    })
                    .Assert();

            // Assert
            action.Should().Throw<AggregatorTestingException>()
                .WithMessage(expectedMessage);
        }

        [Fact]
        public void ForConstructor_DifferentEvent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected event at index 0 to be of type Aggregator.Testing.Tests.TestDomain.PersonNameUpdatedEvent, but got an event of type Aggregator.Testing.Tests.TestDomain.PersonRegisteredEvent instead";

            // Act
            Action action = () =>
                Scenario
                    .ForConstructor(() => Person.Register("Kenny Di Tunnel"))
                    .Then(new PersonNameUpdatedEvent
                    {
                        Name = new UpdatedInfo<string>("A", "B")
                    })
                    .Assert();

            // Assert
            action.Should().Throw<AggregatorTestingException>()
                .WithMessage(expectedMessage);
        }
    }
}
