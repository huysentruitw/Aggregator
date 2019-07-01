using System;

namespace Aggregator.Example.Domain.Events
{
    public sealed class TodoAddedEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
    }

    public sealed class TodoRemovedEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
    }

    public sealed class TodoTitleUpdatedEvent
    {
        public string Id { get; set; }
        public UpdatedInfo<string> Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
    }

    public sealed class TodoDescriptionUpdatedEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public UpdatedInfo<string> Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
    }

    public sealed class TodoDueDateUpdatedEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public UpdatedInfo<DateTimeOffset?> DueDate { get; set; }
    }
}
