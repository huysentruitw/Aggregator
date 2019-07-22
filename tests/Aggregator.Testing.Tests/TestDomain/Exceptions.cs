using System;

namespace Aggregator.Testing.Tests.TestDomain
{
    public class PersonDeletedException : Exception
    {
        public PersonDeletedException(string name)
            : base($"Person with name '{name}' deleted")
        {
            Name = name;
        }

        public string Name { get; }
    }
}
