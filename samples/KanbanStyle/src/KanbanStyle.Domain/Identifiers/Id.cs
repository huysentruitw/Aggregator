using System;
using Aggregator;

namespace KanbanStyle.Domain.Identifiers
{
    internal sealed class Id<TAggregateRoot>
        where TAggregateRoot : AggregateRoot
    {
        private readonly string _name;
        private readonly Guid _value;

        private Id(Guid value)
        {
            _name = typeof(TAggregateRoot).Name;
            _value = value;
        }

        public static Id<TAggregateRoot> New()
            => Guid.NewGuid();

        public static implicit operator Id<TAggregateRoot>(Guid id)
            => new Id<TAggregateRoot>(id);

        public static implicit operator Guid(Id<TAggregateRoot> id)
            => id._value;

        public static implicit operator string(Id<TAggregateRoot> id)
            => id.ToString();

        public override string ToString()
            => $"{_name}.{_value:N}";

        private bool Equals(Id<TAggregateRoot> other)
            => _name == other._name && _value.Equals(other._value);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || obj is Id<TAggregateRoot> other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(_name, _value);
    }
}
