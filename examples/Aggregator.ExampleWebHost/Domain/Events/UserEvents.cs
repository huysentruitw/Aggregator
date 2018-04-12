using System;

namespace Aggregator.ExampleWebHost.Domain.Events
{
    internal class CreatedUserEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateCreatedUtc { get; set; }
    }

    internal class UpdatedUserEmailAddressEvent
    {
        public Guid Id { get; set; }

        public UpdatedInfo<string> EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateUpdatedUtc { get; set; }
    }

    internal class UpdatedUserGivenNameEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public UpdatedInfo<string> GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateUpdatedUtc { get; set; }
    }

    internal class UpdatedUserSurnameEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public UpdatedInfo<string> Surname { get; set; }

        public DateTimeOffset DateUpdatedUtc { get; set; }
    }

    internal class DeletedUserEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateDeletedUtc { get; set; }
    }
}
