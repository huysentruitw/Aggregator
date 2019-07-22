namespace Aggregator.Testing.Tests.TestDomain
{
    public sealed class UpdatedInfo<T>
    {
        public UpdatedInfo(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; }

        public T NewValue { get; }
    }
}
