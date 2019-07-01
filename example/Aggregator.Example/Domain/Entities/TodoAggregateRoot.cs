using System;
using Aggregator.Example.Domain.Events;
using Aggregator.Example.Domain.Exceptions;

namespace Aggregator.Example.Domain.Entities
{
    public sealed class TodoAggregateRoot : AggregateRoot
    {
        private string _id;
        private string _title;
        private string _description;
        private DateTimeOffset? _dueDate;
        private bool _isRemoved;

        public TodoAggregateRoot()
        {
            Register<TodoAddedEvent>(@event =>
            {
                _id = @event.Id;
                _title = @event.Title;
                _description = @event.Description;
                _dueDate = @event.DueDate;
                _isRemoved = false;
            });

            Register<TodoRemovedEvent>(@event =>
            {
                GuardRemoved();
                _isRemoved = true;
            });

            Register<TodoTitleUpdatedEvent>(@event =>
            {
                GuardRemoved();
                _title = @event.Title.NewValue;
            });

            Register<TodoDescriptionUpdatedEvent>(@event =>
            {
                GuardRemoved();
                _description = @event.Description.NewValue;
            });

            Register<TodoDueDateUpdatedEvent>(@event =>
            {
                GuardRemoved();
                _dueDate = @event.DueDate.NewValue;
            });
        }

        public static TodoAggregateRoot Create(string id, string title, string description, DateTimeOffset? dueDate)
        {
            var aggregate = new TodoAggregateRoot();
            aggregate.Apply(new TodoAddedEvent
            {
                Id = id,
                Title = title,
                Description = description,
                DueDate = dueDate
            });
            return aggregate;
        }

        public void Remove()
        {
            Apply(new TodoRemovedEvent
            {
                Id = _id,
                Title = _title,
                Description = _description,
                DueDate = _dueDate
            });
        }

        public void UpdateTitle(string title)
        {
            if (_title.Equals(title))
                return;

            Apply(new TodoTitleUpdatedEvent
            {
                Id = _id,
                Title = new UpdatedInfo<string>(_title, title),
                Description = _description,
                DueDate = _dueDate
            });
        }

        public void UpdateDescription(string description)
        {
            if (string.Equals(description, _description))
                return;

            Apply(new TodoDescriptionUpdatedEvent
            {
                Id = _id,
                Title = _title,
                Description = new UpdatedInfo<string>(_description, description),
                DueDate = _dueDate
            });
        }

        public void UpdateDueDate(DateTimeOffset? dueDate)
        {
            if (_dueDate.Equals(dueDate))
                return;

            Apply(new TodoDueDateUpdatedEvent
            {
                Id = _id,
                Title = _title,
                Description = _description,
                DueDate = new UpdatedInfo<DateTimeOffset?>(_dueDate, dueDate)
            });
        }

        private void GuardRemoved()
        {
            if (_isRemoved)
                throw new AggregateRootAlreadyRemovedException();
        }
    }
}
