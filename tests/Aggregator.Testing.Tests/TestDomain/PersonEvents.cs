namespace Aggregator.Testing.Tests.TestDomain
{
    public class PersonRegisteredEvent
    {
        public string Name { get; set; }
    }

    public class PersonNameUpdatedEvent
    {
        public UpdatedInfo<string> Name { get; set; }
    }

    public class PersonDeletedEvent
    {
        public string Name { get; set; }
    }
}
