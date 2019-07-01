using System;

namespace Aggregator.Example.Domain
{
    public sealed class AddTodoCommand
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
    }

    public sealed class RemoveTodoCommand
    {
        public Guid Id { get; set; }
    }

    public sealed class UpdateTodoTitleCommand
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
    }

    public sealed class UpdateTodoDescriptionCommand
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
    }

    public sealed class UpdateTodoDueDateCommand
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DueDate { get; set; }
    }
}
