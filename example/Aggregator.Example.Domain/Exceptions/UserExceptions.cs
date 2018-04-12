using Aggregator.Exceptions;
using Aggregator.Example.Domain.Entities;

namespace Aggregator.Example.Domain.Exceptions
{
    internal class UserDeletedException : AggregateRootException<string>
    {
        public UserDeletedException(AggregateRootId<User> identifier)
            : base(identifier, "User deleted")
        {
        }
    }
}
