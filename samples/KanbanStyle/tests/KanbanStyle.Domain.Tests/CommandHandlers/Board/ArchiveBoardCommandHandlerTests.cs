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
    public sealed class ArchiveBoardCommandHandlerTests
    {
        private readonly Mock<IUtcNowFactory> _utcNowFactoryMock = new Mock<IUtcNowFactory>();

        public ArchiveBoardCommandHandlerTests()
        {
            _utcNowFactoryMock.SetupGet(x => x.UtcNow).Returns(Model.DateUtc);
        }

        [Fact]
        public void Handle_EmptyId_ShouldThrowValidationException()
        {
            // Arrange
            var command = new ArchiveBoard { Id = Guid.Empty };
            var handler = new ArchiveBoardCommandHandler(Mock.Of<IRepository<Board>>(), _utcNowFactoryMock.Object);

            // Act
            Func<Task> action = () => handler.Handle(command, cancellationToken: default);

            // Assert
            action.Should().Throw<ValidationException>();
        }

        [Fact]
        public async Task Handle_ValidId_ShouldArchiveCorrectBoard()
        {
            // Arrange
            var command = new ArchiveBoard { Id = Model.Id };
            var boardMock = new Mock<Board>();
            var repositoryMock = new Mock<IRepository<Board>>();
            repositoryMock.Setup(x => x.Get(It.IsAny<string>())).ReturnsAsync(boardMock.Object);
            var handler = new ArchiveBoardCommandHandler(repositoryMock.Object, _utcNowFactoryMock.Object);

            // Act
            await handler.Handle(command, cancellationToken: default);

            // Assert
            repositoryMock.Verify(x => x.Get(Model.Id), Times.Once);
            boardMock.Verify(x => x.Archive(Model.DateUtc), Times.Once);
        }

        private static class Model
        {
            public static readonly BoardId Id = BoardId.New();

            public static readonly DateTime DateUtc = DateTime.UtcNow;
        }
    }
}
