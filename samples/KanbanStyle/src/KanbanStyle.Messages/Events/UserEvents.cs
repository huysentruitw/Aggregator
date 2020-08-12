using System;

namespace KanbanStyle.Domain.Messages
{
    public sealed class UserRegistered
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public DateTime DateCreatedUtc { get; set; }
    }

    public sealed class UserDeleted
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public DateTime DateDeletedUtc { get; set; }
    }
}
