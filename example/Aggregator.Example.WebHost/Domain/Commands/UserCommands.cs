using System;

namespace Aggregator.Example.WebHost.Domain.Commands
{
    internal sealed class CreateUserCommand
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }
    }

    internal sealed class UpdateUserEmailAddressCommand
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }
    }

    internal sealed class UpdateUserGivenNameCommand
    {
        public Guid Id { get; set; }

        public string GivenName { get; set; }
    }

    internal sealed class UpdateUserSurnameCommand
    {
        public Guid Id { get; set; }

        public string Surname { get; set; }
    }

    internal sealed class DeleteUserCommand
    {
        public Guid Id { get; set; }
    }
}
