namespace Aggregator.Example.Domain.Events
{
    public sealed class UpdatedInfo<T>
    {
        public UpdatedInfo(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; set; }
        public T NewValue { get; set; }
    }
}
