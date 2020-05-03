using System;
using System.Threading.Tasks;
using Aggregator.Persistence;
using FluentAssertions;
using FluentValidation;
using KanbanStyle.Domain.CommandHandlers;
using KanbanStyle.Domain.Entities;
using KanbanStyle.Domain.Identifiers;
using KanbanStyle.Domain.Messages;
using Moq;
using Xunit;

namespace KanbanStyle.Domain.Tests.CommandHandlers
{
    public sealed class CreateBoardCommandHandlerTests
    {
        private readonly Mock<IUtcNowFactory> _utcNowFactoryMock = new Mock<IUtcNowFactory>();

        public CreateBoardCommandHandlerTests()
        {
            _utcNowFactoryMock.SetupGet(x => x.UtcNow).Returns(Model.DateUtc);
        }

        [Fact]
        public void Handle_EmptyId_ShouldThrowValidationException()
        {
            // Arrange
            var command = new CreateBoard
            {
                Id = Guid.Empty,
                Name = Model.Name,
            };
            var handler = new CreateBoardCommandHandler(Mock.Of<IRepository<Board>>(), _utcNowFactoryMock.Object);

            // Act
            Func<Task> action = () => handler.Handle(command, cancellationToken: default);

            // Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("*Id*");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Handle_NullOrEmptyName_ShouldThrowValidationException(string name)
        {
            // Arrange
            var command = new CreateBoard
            {
                Id = Model.Id,
                Name = name,
            };
            var handler = new CreateBoardCommandHandler(Mock.Of<IRepository<Board>>(), _utcNowFactoryMock.Object);

            // Act
            Func<Task> action = () => handler.Handle(command, cancellationToken: default);

            // Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("*Name*");
        }

        [Fact]
        public async Task Handle_ValidParameters_ShouldCreateNewBoard()
        {
            // Arrange
            var command = new CreateBoard
            {
                Id = Model.Id,
                Name = Model.Name,
            };
            var repositoryMock = new Mock<IRepository<Board>>();
            var handler = new CreateBoardCommandHandler(repositoryMock.Object, _utcNowFactoryMock.Object);

            // Act
            await handler.Handle(command, cancellationToken: default);

            // Assert
            repositoryMock.Verify(x => x.Add(Model.Id, It.IsAny<Board>()), Times.Once);
        }

        private static class Model
        {
            public static readonly Id<Board> Id = Id<Board>.New();

            public const string Name = "My board";

            public static readonly DateTime DateUtc = DateTime.UtcNow;
        }
    }
}
