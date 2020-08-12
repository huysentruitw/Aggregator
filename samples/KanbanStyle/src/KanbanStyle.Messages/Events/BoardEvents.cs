using System;

namespace KanbanStyle.Domain.Messages
{
    public sealed class BoardCreated
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime DateCreatedUtc { get; set; }
    }

    public sealed class BoardNameUpdated
    {
        public Guid Id { get; set; }

        public UpdatedInfo<string> Name { get; set; }

        public DateTime DateUpdatedUtc { get; set; }
    }

    public sealed class BoardArchived
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime DateArchivedUtc { get; set; }
    }
}
