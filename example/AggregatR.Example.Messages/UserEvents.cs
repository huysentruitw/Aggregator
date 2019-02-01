using System;

namespace AggregatR.Example.Messages
{
    public class CreatedUserEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateCreatedUtc { get; set; }
    }

    public class UpdatedUserEmailAddressEvent
    {
        public Guid Id { get; set; }

        public UpdatedInfo<string> EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateUpdatedUtc { get; set; }
    }

    public class UpdatedUserGivenNameEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public UpdatedInfo<string> GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateUpdatedUtc { get; set; }
    }

    public class UpdatedUserSurnameEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public UpdatedInfo<string> Surname { get; set; }

        public DateTimeOffset DateUpdatedUtc { get; set; }
    }

    public class DeletedUserEvent
    {
        public Guid Id { get; set; }

        public string EmailAddress { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public DateTimeOffset DateDeletedUtc { get; set; }
    }
}
