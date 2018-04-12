using Aggregator.Exceptions;
using Aggregator.Example.WebHost.Domain.Entities;

namespace Aggregator.Example.WebHost.Domain.Exceptions
{
    internal class UserDeletedException : AggregateRootException<string>
    {
        public UserDeletedException(AggregateRootId<User> identifier)
            : base(identifier, "User deleted")
        {
        }
    }
}
