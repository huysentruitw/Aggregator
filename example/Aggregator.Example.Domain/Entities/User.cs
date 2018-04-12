using System;
using Aggregator.Example.Domain.Exceptions;
using Aggregator.Example.Messages;

namespace Aggregator.Example.Domain.Entities
{
    internal sealed class User : AggregateRoot
    {
        private AggregateRootId<User> _id;
        private string _emailAddress;
        private string _givenName;
        private string _surname;
        private bool _isDeleted;

        // This constructor must be part of any constructor chain
        public User()
        {
            Register<CreatedUserEvent>(OnCreated);
            Register<UpdatedUserEmailAddressEvent>(OnEmailAddressUpdated);
            Register<UpdatedUserGivenNameEvent>(OnGivenNameUpdated);
            Register<UpdatedUserSurnameEvent>(OnSurnameUpdated);
            Register<DeletedUserEvent>(OnDeleted);
        }

        private User(CreatedUserEvent @event) : this()
        {
            Apply(@event);
        }

        public static User Create(AggregateRootId<User> id, string emailAddress, string givenName, string surname)
            => new User(new CreatedUserEvent
            {
                Id = id,
                EmailAddress = emailAddress,
                GivenName = givenName,
                Surname = surname,
                DateCreatedUtc = DateTimeOffset.UtcNow
            });

        public void SetEmailAddress(string emailAddress)
        {
            GuardDeleted();

            if (_emailAddress.Equals(emailAddress)) return;

            Apply(new UpdatedUserEmailAddressEvent
            {
                Id = _id,
                EmailAddress = UpdatedInfo.From(_emailAddress).To(emailAddress),
                GivenName = _givenName,
                Surname = _surname,
                DateUpdatedUtc = DateTimeOffset.UtcNow
            });
        }

        public void SetGivenName(string givenName)
        {
            GuardDeleted();

            if (_givenName.Equals(givenName)) return;

            Apply(new UpdatedUserGivenNameEvent
            {
                Id = _id,
                EmailAddress = _emailAddress,
                GivenName = UpdatedInfo.From(_givenName).To(givenName),
                Surname = _surname,
                DateUpdatedUtc = DateTimeOffset.UtcNow
            });
        }

        public void SetSurname(string surname)
        {
            GuardDeleted();

            if (_surname.Equals(surname)) return;

            Apply(new UpdatedUserSurnameEvent
            {
                Id = _id,
                EmailAddress = _emailAddress,
                GivenName = _givenName,
                Surname = UpdatedInfo.From(_surname).To(surname),
                DateUpdatedUtc = DateTimeOffset.UtcNow
            });
        }

        public void Delete()
        {
            GuardDeleted();

            Apply(new DeletedUserEvent
            {
                Id = _id,
                EmailAddress = _emailAddress,
                GivenName = _givenName,
                Surname = _surname,
                DateDeletedUtc = DateTimeOffset.UtcNow
            });
        }

        private void OnCreated(CreatedUserEvent @event)
        {
            _id = @event.Id;
            _emailAddress = @event.EmailAddress;
            _givenName = @event.GivenName;
            _surname = @event.Surname;
        }

        private void OnEmailAddressUpdated(UpdatedUserEmailAddressEvent @event)
        {
            _emailAddress = @event.EmailAddress.NewValue;
        }

        private void OnGivenNameUpdated(UpdatedUserGivenNameEvent @event)
        {
            _givenName = @event.GivenName.NewValue;
        }

        private void OnSurnameUpdated(UpdatedUserSurnameEvent @event)
        {
            _surname = @event.Surname.NewValue;
        }

        private void OnDeleted(DeletedUserEvent @event)
        {
            _isDeleted = true;
        }

        private void GuardDeleted()
        {
            if (_isDeleted) throw new UserDeletedException(_id);
        }
    }
}
