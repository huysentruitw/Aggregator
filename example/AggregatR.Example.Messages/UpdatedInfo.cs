namespace AggregatR.Example.Messages
{
    public class UpdatedInfo<T>
    {
        public UpdatedInfo(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; }

        public T NewValue { get; }
    }

    public static class UpdatedInfo
    {
        public static UpdatedInfoBuilder<T> From<T>(T oldValue)
            => new UpdatedInfoBuilder<T>(oldValue);
    }

    public class UpdatedInfoBuilder<T>
    {
        private readonly T _oldValue;

        public UpdatedInfoBuilder(T oldValue)
        {
            _oldValue = oldValue;
        }

        public UpdatedInfo<T> To(T newValue)
            => new UpdatedInfo<T>(_oldValue, newValue);
    }
}
