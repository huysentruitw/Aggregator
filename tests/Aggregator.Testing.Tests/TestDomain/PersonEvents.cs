using System.Diagnostics.CodeAnalysis;

namespace Aggregator.Testing.Tests.TestDomain
{
    [SuppressMessage("ReSharper", "SA1649", Justification = "Allow multiple person related events in single file")]
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
