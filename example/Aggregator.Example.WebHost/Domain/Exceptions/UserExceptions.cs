using Aggregator.Exceptions;
using Aggregator.ExampleWebHost.Domain.Entities;

namespace Aggregator.ExampleWebHost.Domain.Exceptions
{
    internal class UserDeletedException : AggregateRootException<string>
    {
        public UserDeletedException(AggregateRootId<User> identifier)
            : base(identifier, "User deleted")
        {
        }
    }
}
