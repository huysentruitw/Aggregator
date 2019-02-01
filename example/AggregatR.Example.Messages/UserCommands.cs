using System;

namespace AggregatR.Example.Messages
{
    public sealed class CreateUserCommand
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }
    }

    public sealed class UpdateUserEmailAddressCommand
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }
    }

    public sealed class UpdateUserGivenNameCommand
    {
        public Guid Id { get; set; }

        public string GivenName { get; set; }
    }

    public sealed class UpdateUserSurnameCommand
    {
        public Guid Id { get; set; }

        public string Surname { get; set; }
    }

    public sealed class DeleteUserCommand
    {
        public Guid Id { get; set; }
    }
}
