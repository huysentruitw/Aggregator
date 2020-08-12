using System;

namespace KanbanStyle.Domain.Messages
{
    public sealed class CreateBoard
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
    }

    public sealed class UpdateBoardName
    {
        public Guid Id { get; set; }

        public string NewName { get; set; }
    }

    public sealed class ArchiveBoard
    {
        public Guid Id { get; set; }
    }
}
