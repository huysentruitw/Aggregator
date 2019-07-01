using System;

namespace Aggregator.Testing.Tests.TestDomain
{
    public class Person : AggregateRoot
    {
        private string _name;
        private bool _isDeleted;

        private Person()
        {
            Register<PersonRegisteredEvent>(e => { _name = e.Name; _isDeleted = false; });
            Register<PersonNameUpdatedEvent>(e => { _name = e.Name.NewValue; });
            Register<PersonDeletedEvent>(e => { _isDeleted = true; });
        }

        private Person(PersonRegisteredEvent createdEvent)
            : this()
        {
            Apply(createdEvent);
        }

        public static Person Factory() => new Person();

        public static Person Register(string name)
            => new Person(new PersonRegisteredEvent { Name = name });

        public void UpdateName(string name)
        {
            GuardDeleted();

            if (object.Equals(_name, name))
                return;

            Apply(new PersonNameUpdatedEvent
            {
                Name = new UpdatedInfo<string>(_name, name)
            });
        }

        public void Delete()
        {
            GuardDeleted();

            Apply(new PersonDeletedEvent
            {
                Name = _name
            });
        }

        private void GuardDeleted()
        {
            if (_isDeleted)
                throw new PersonDeletedException(_name);
        }
    }
}
