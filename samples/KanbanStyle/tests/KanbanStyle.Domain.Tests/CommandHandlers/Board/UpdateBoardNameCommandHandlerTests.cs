using System;
using System.Threading.Tasks;
using Aggregator.Persistence;
using FluentAssertions;
using FluentValidation;
using KanbanStyle.Domain.CommandHandlers;
using KanbanStyle.Domain.Entities;
using KanbanStyle.Domain.Messages;
using Moq;
using Xunit;

namespace KanbanStyle.Domain.Tests.CommandHandlers
{
    public sealed class UpdateBoardNameCommandHandlerTests
    {
        private readonly Mock<IUtcNowFactory> _utcNowFactoryMock = new Mock<IUtcNowFactory>();

        public UpdateBoardNameCommandHandlerTests()
        {
            _utcNowFactoryMock.SetupGet(x => x.UtcNow).Returns(Model.DateUtc);
        }

        [Fact]
        public void Handle_EmptyId_ShouldThrowValidationException()
        {
            // Arrange
            var command = new UpdateBoardName
            {
                Id = Guid.Empty,
                NewName = Model.NewName,
            };
            var handler = new UpdateBoardNameCommandHandler(Mock.Of<IRepository<Board>>(), _utcNowFactoryMock.Object);

            // Act
            Func<Task> action = () => handler.Handle(command, cancellationToken: default);

            // Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("*Id*");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Handle_NewNameNullOrEmpty_ShouldThrowValidationException(string newName)
        {
            // Arrange
            var command = new UpdateBoardName
            {
                Id = Model.Id,
                NewName = newName,
            };
            var handler = new UpdateBoardNameCommandHandler(Mock.Of<IRepository<Board>>(), _utcNowFactoryMock.Object);

            // Act
            Func<Task> action = () => handler.Handle(command, cancellationToken: default);

            // Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("*NewName*");
        }

        [Fact]
        public async Task Handle_ValidParameters_ShouldUpdateNameOfCorrectBoard()
        {
            // Arrange
            var command = new UpdateBoardName
            {
                Id = Model.Id,
                NewName = Model.NewName,
            };
            var boardMock = new Mock<Board>();
            var repositoryMock = new Mock<IRepository<Board>>();
            repositoryMock.Setup(x => x.Get(It.IsAny<string>())).ReturnsAsync(boardMock.Object);
            var handler = new UpdateBoardNameCommandHandler(repositoryMock.Object, _utcNowFactoryMock.Object);

            // Act
            await handler.Handle(command, cancellationToken: default);

            // Assert
            repositoryMock.Verify(x => x.Get(Model.Id), Times.Once);
            boardMock.Verify(x => x.UpdateName(Model.NewName, Model.DateUtc), Times.Once);
        }

        private static class Model
        {
            public static readonly Id<Board> Id = Id<Board>.New();

            public const string NewName = "New board name";

            public static readonly DateTime DateUtc = DateTime.UtcNow;
        }
    }
}
