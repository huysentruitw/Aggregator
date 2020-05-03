using System;
using Aggregator.Testing;
using AutoFixture;
using KanbanStyle.Domain.Entities;
using KanbanStyle.Domain.Exceptions;
using KanbanStyle.Domain.Identifiers;
using KanbanStyle.Domain.Messages;
using Xunit;

namespace KanbanStyle.Domain.Tests.Entities
{
    public sealed class BoardTests
    {
        private static Fixture Fixture = new Fixture();

        [Fact]
        public void Create_ShouldApplyBoardCreatedEvent()
        {
            Scenario.ForConstructor(() => Board.Create(Model.Id, Model.Name, Model.DateUtc))
                .Then(new BoardCreated
                {
                    Id = Model.Id,
                    Name = Model.Name,
                    DateCreatedUtc = Model.DateUtc,
                })
                .Assert();
        }

        [Fact]
        public void UpdateName_ShouldApplyBoardNameUpdatedEvent()
        {
            Scenario.ForCommand(() => new Board())
                .Given(new BoardCreated
                {
                    Id = Model.Id,
                    Name = Model.Name,
                    DateCreatedUtc = Model.DateUtc,
                })
                .When(board => board.UpdateName(Model.NewName, Model.DateUtc))
                .Then(new BoardNameUpdated
                {
                    Id = Model.Id,
                    Name = UpdatedInfo.From(Model.Name).To(Model.NewName),
                    DateUpdatedUtc = Model.DateUtc,
                })
                .Assert();
        }

        [Fact]
        public void Archive_ShouldApplyBoardArchivedEvent()
        {
            Scenario.ForCommand(() => new Board())
                .Given(new BoardCreated
                {
                    Id = Model.Id,
                    Name = Model.Name,
                    DateCreatedUtc = Model.DateUtc,
                })
                .When(board => board.Archive(Model.DateUtc))
                .Then(new BoardArchived
                {
                    Id = Model.Id,
                    Name = Model.Name,
                    DateArchivedUtc = Model.DateUtc,
                })
                .Assert();
        }

        [Fact]
        public void Archive_AlreadyArchivedBoard_ShouldThrowException()
        {
            Scenario.ForCommand(() => new Board())
                .Given(
                    new BoardCreated
                    {
                        Id = Model.Id,
                        Name = Model.Name,
                        DateCreatedUtc = Model.DateUtc,
                    },
                    new BoardArchived
                    {
                        Id = Model.Id,
                        Name = Model.Name,
                        DateArchivedUtc = Model.DateUtc,
                    })
                .When(board => board.Archive(Model.DateUtc))
                .Throws<OperationNotAllowedOnArchivedBoardException>()
                .Assert();
        }

        private static class Model
        {
            public static readonly Id<Board> Id = Id<Board>.New();

            public const string Name = "My board";

            public const string NewName = "My new board";

            public static readonly DateTime DateUtc = DateTime.UtcNow;
        }
    }
}
