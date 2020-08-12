using System;
using Aggregator;
using KanbanStyle.Domain.Exceptions;
using KanbanStyle.Domain.Messages;

namespace KanbanStyle.Domain.Entities
{
    internal class User : AggregateRoot
    {
        private Id<User> _id;
        private string _name;
        private string _email;
        private bool _isDeleted;

        public User()
        {
            Register<UserRegistered>(@event =>
            {
                _id = @event.Id;
                _name = @event.Name;
                _email = @event.Email;
                _isDeleted = false;
            });

            Register<UserDeleted>(@event =>
            {
                _isDeleted = true;
            });
        }

        private User(UserRegistered userRegistered)
            : this()
        {
            Apply(userRegistered);
        }

        public static User Register(Id<User> id, string name, string email, DateTime dateRegisteredUtc)
            => new User(new UserRegistered
            {
                Id = id,
                Name = name,
                Email = email,
                DateCreatedUtc = dateRegisteredUtc,
            });

        public void Delete(DateTime dateDeletedUtc)
        {
            GuardDeleted();

            Apply(new UserDeleted
            {
                Id = _id,
                Name = _name,
                Email = _email,
                DateDeletedUtc = dateDeletedUtc,
            });
        }

        private void GuardDeleted()
        {
            if (_isDeleted)
                throw new OperationNotAllowedOnDeletedUserException();
        }
    }
}
