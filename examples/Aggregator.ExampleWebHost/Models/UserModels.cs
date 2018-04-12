using System;

namespace Aggregator.ExampleWebHost.Models
{
    public class UserModel
    {
        public Guid Id { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string EmailAddress { get; set; }

        public UserModel() { }

        public UserModel(Guid id, NewUserModel model)
        {
            Id = id;
            GivenName = model.GivenName;
            Surname = model.Surname;
            EmailAddress = model.EmailAddress;
        }
    }

    public class NewUserModel
    {
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string EmailAddress { get; set; }
    }
}
