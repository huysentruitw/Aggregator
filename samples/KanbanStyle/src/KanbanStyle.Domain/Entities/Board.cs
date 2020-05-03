using System;
using Aggregator;
using KanbanStyle.Domain.Exceptions;
using KanbanStyle.Domain.Identifiers;
using KanbanStyle.Domain.Messages;

namespace KanbanStyle.Domain.Entities
{
    internal class Board : AggregateRoot
    {
        private BoardId _id;
        private string _name;
        private bool _archived;

        public Board()
        {
            Register<BoardCreated>(@event =>
            {
                _id = @event.Id;
                _name = @event.Name;
                _archived = false;
            });

            Register<BoardNameUpdated>(@event =>
            {
                _name = @event.Name.NewValue;
            });

            Register<BoardArchived>(@event =>
            {
                _archived = true;
            });
        }

        private Board(BoardCreated boardCreated)
            : this()
        {
            Apply(boardCreated);
        }

        public static Board Create(BoardId id, string name, DateTime dateCreatedUtc)
            => new Board(new BoardCreated
            {
                Id = id,
                Name = name,
                DateCreatedUtc = dateCreatedUtc,
            });

        public virtual void UpdateName(string newName, DateTime dateUpdatedUtc)
        {
            if (_name.Equals(newName))
                return;

            Apply(new BoardNameUpdated
            {
                Id = _id,
                Name = UpdatedInfo.From(_name).To(newName),
                DateUpdatedUtc = dateUpdatedUtc,
            });
        }

        public virtual void Archive(DateTime dateArchivedUtc)
        {
            GuardArchived();

            Apply(new BoardArchived
            {
                Id = _id,
                Name = _name,
                DateArchivedUtc = dateArchivedUtc,
            });
        }

        private void GuardArchived()
        {
            if (_archived)
                throw new OperationNotAllowedOnArchivedBoardException();
        }
    }
}
