using System;
using System.Diagnostics.CodeAnalysis;

namespace Aggregator.Testing.Tests.TestDomain
{
    [SuppressMessage("ReSharper", "SA1649", Justification = "Allow multiple exceptions in single file")]
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
