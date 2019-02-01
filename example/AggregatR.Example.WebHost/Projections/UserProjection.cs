using System;
using System.Collections.Generic;
using System.Linq;
using AggregatR.Example.Messages;
using AggregatR.Example.WebHost.Projections.Infrastructure;

namespace AggregatR.Example.WebHost.Projections
{
    public interface IUserStore
    {
        User[] GetUsers();
    }

    internal sealed class UserProjection : ProjectionBase, IUserStore
    {
        private readonly Dictionary<Guid, User> _userStore = new Dictionary<Guid, User>();

        public UserProjection()
        {
            When<CreatedUserEvent>(@event => _userStore.TryAdd(@event.Id, new User
            {
                Id = @event.Id,
                GivenName = @event.GivenName,
                Surname = @event.Surname,
                EmailAddress = @event.EmailAddress,
                DateCreatedUtc = @event.DateCreatedUtc
            }));

            When<UpdatedUserGivenNameEvent>(@event =>
            {
                if (!_userStore.TryGetValue(@event.Id, out var user)) return;
                user.GivenName = @event.GivenName.NewValue;
                user.DateUpdatedUtc = @event.DateUpdatedUtc;
            });

            When<UpdatedUserSurnameEvent>(@event =>
            {
                if (!_userStore.TryGetValue(@event.Id, out var user)) return;
                user.Surname = @event.Surname.NewValue;
                user.DateUpdatedUtc = @event.DateUpdatedUtc;
            });

            When<UpdatedUserEmailAddressEvent>(@event =>
            {
                if (!_userStore.TryGetValue(@event.Id, out var user)) return;
                user.EmailAddress = @event.EmailAddress.NewValue;
                user.DateUpdatedUtc = @event.DateUpdatedUtc;
            });

            When<DeletedUserEvent>(@event => _userStore.Remove(@event.Id));
        }

        public User[] GetUsers()
            => _userStore.Values
                .OrderBy(x => x.GivenName)
                .OrderBy(x => x.Surname)
                .ToArray();
    }

    public class User
    {
        public Guid Id { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string EmailAddress { get; set; }
        public DateTimeOffset DateCreatedUtc { get; set; }
        public DateTimeOffset? DateUpdatedUtc { get; set; }
    }
}
