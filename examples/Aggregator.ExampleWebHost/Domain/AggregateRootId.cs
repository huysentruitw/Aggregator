using System;

namespace Aggregator.ExampleWebHost.Domain
{
    internal class AggregateRootId
    {
        public AggregateRootId(string type, Guid id)
        {
            Type = type;
            Id = id;
        }

        public string Type { get; }

        public Guid Id { get; }

        public override string ToString()
            => $"{Type}:{Id:N}";

        public static implicit operator string(AggregateRootId aggregateRootId)
            => aggregateRootId.ToString();

        public static implicit operator Guid(AggregateRootId aggregateRootId)
            => aggregateRootId.Id;

        public static implicit operator AggregateRootId(string aggregateRootId)
        {
            if (aggregateRootId == null) throw new ArgumentNullException(nameof(aggregateRootId));
            var parts = aggregateRootId.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !Guid.TryParse(parts[1], out var id))
                throw new ArgumentException("Invalid format", nameof(aggregateRootId));
            return new AggregateRootId(parts[0], id);
        }
    }

    internal class AggregateRootId<TAggregateRoot> : AggregateRootId
        where TAggregateRoot : AggregateRoot<object>
    {
        public AggregateRootId(Guid id)
            : base(typeof(TAggregateRoot).Name, id)
        {
        }

        public static implicit operator AggregateRootId<TAggregateRoot>(Guid id)
            => new AggregateRootId<TAggregateRoot>(id);
    }
}
