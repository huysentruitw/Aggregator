using System;
using Aggregator.Testing;
using KanbanStyle.Domain.Entities;
using KanbanStyle.Domain.Exceptions;
using KanbanStyle.Domain.Messages;
using Xunit;

namespace KanbanStyle.Domain.Tests.Entities
{
    public sealed class PersonTests
    {
        [Fact]
        public void Create_ShouldApplyPersonCreatedEvent()
        {
            Scenario.ForConstructor(() => User.Register(Model.Id, Model.Name, Model.Email, Model.DateUtc))
                .Then(new UserRegistered
                {
                    Id = Model.Id,
                    Name = Model.Name,
                    Email = Model.Email,
                    DateCreatedUtc = Model.DateUtc,
                })
                .Assert();
        }

        [Fact]
        public void Delete_ShouldApplyUserDeletedEvent()
        {
            Scenario.ForCommand(() => new User())
                .Given(new UserRegistered
                {
                    Id = Model.Id,
                    Name = Model.Name,
                    Email = Model.Email,
                    DateCreatedUtc = Model.DateUtc,
                })
                .When(user => user.Delete(Model.DateUtc))
                .Then(new UserDeleted
                {
                    Id = Model.Id,
                    Name = Model.Name,
                    Email = Model.Email,
                    DateDeletedUtc = Model.DateUtc,
                })
                .Assert();
        }

        [Fact]
        public void Delete_AlreadyDeletedUser_ShouldThrowException()
        {
            Scenario.ForCommand(() => new User())
                .Given(
                    new UserRegistered
                    {
                        Id = Model.Id,
                        Name = Model.Name,
                        Email = Model.Email,
                        DateCreatedUtc = Model.DateUtc,
                    },
                    new UserDeleted
                    {
                        Id = Model.Id,
                        Name = Model.Name,
                        Email = Model.Email,
                        DateDeletedUtc = Model.DateUtc,
                    })
                .When(user => user.Delete(Model.DateUtc))
                .Throws<OperationNotAllowedOnDeletedUserException>()
                .Assert();
        }

        private static class Model
        {
            public static readonly Id<User> Id = Id<User>.New();

            public const string Name = "George Groomy";

            public const string Email = "george.groomy@gmail.com";

            public static readonly DateTime DateUtc = DateTime.UtcNow;
        }
    }
}
