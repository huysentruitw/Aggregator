using AggregatR.Exceptions;
using AggregatR.Example.Domain.Entities;

namespace AggregatR.Example.Domain.Exceptions
{
    internal class UserDeletedException : AggregateRootException<string>
    {
        public UserDeletedException(AggregateRootId<User> identifier)
            : base(identifier, "User deleted")
        {
        }
    }
}
