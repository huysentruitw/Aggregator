using System;
using System.Text;
using Aggregator.Testing.Tests.TestDomain;
using NUnit.Framework;

namespace Aggregator.Testing.Tests
{
    [TestFixture]
    public sealed partial class ScenarioTests
    {
        [Test]
        public void ForConstructor_ShouldReturnConstructorContinuation()
        {
            // Act
            var constructorContinuation = Scenario.ForConstructor(() => Person.Register("Indy Struyck"));

            // Assert
            Assert.That(constructorContinuation, Is.Not.Null);
        }

        [Test]
        public void ForConstructor_ConstructorIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => Scenario.ForConstructor<Person>(null));
            Assert.That(exception.ParamName, Is.EqualTo("constructor"));
        }

        [Test]
        public void ForConstructor_ExpectedEvent_Assert_ShouldNotThrowException()
        {
            // Act
            TestDelegate action = () =>
                Scenario
                    .ForConstructor(() => Person.Register("Kenny Di Tunnel"))
                    .Then(new PersonRegisteredEvent
                    {
                        Name = "Kenny Di Tunnel"
                    })
                    .Assert();

            // Assert
            Assert.DoesNotThrow(action);
        }

        [Test]
        public void ForConstructor_ExpectedEventWithWrongContent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = new StringBuilder();
            expectedMessage.AppendLine("Expected member Name to be ");
            expectedMessage.AppendLine("\"Marieke Rechte\" with a length of 14, but ");
            expectedMessage.Append("\"Kenny Di Tunnel\" has a length of 15");

            // Act
            TestDelegate action = () =>
                Scenario
                    .ForConstructor(() => Person.Register("Kenny Di Tunnel"))
                    .Then(new PersonRegisteredEvent
                    {
                        Name = "Marieke Rechte"
                    })
                    .Assert();

            // Assert
            var exception = Assert.Throws<AssertionException>(action);
            Assert.That(exception.Message, Does.StartWith(expectedMessage.ToString()));
        }

        [Test]
        public void ForConstructor_DifferentEvent_Assert_ShouldThrowException()
        {
            // Arrange
            var expectedMessage = "Expected type to be Aggregator.Testing.Tests.TestDomain.PersonNameUpdatedEvent, but found Aggregator.Testing.Tests.TestDomain.PersonRegisteredEvent";
            
            // Act
            TestDelegate action = () =>
                Scenario
                    .ForConstructor(() => Person.Register("Kenny Di Tunnel"))
                    .Then(new PersonNameUpdatedEvent
                    {
                        Name = new UpdatedInfo<string>("A", "B")
                    })
                    .Assert();

            // Assert
            var exception = Assert.Throws<AssertionException>(action);
            Assert.That(exception.Message, Does.StartWith(expectedMessage.ToString()));
        }
    }
}
