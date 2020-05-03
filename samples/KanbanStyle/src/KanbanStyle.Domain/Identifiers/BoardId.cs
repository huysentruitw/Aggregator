using System;

namespace KanbanStyle.Domain.Identifiers
{
    internal sealed class BoardId
    {
        private readonly Guid _value;

        private BoardId(Guid value)
        {
            _value = value;
        }

        public static BoardId New()
            => Guid.NewGuid();

        public static implicit operator BoardId(Guid id)
            => new BoardId(id);

        public static implicit operator Guid(BoardId id)
            => id._value;

        public static implicit operator string(BoardId id)
            => id.ToString();

        public override string ToString()
            => $"Board.{_value:N}";

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is BoardId other && Equals(other);
        }

        private bool Equals(BoardId other)
        {
            return _value.Equals(other._value);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }
}
