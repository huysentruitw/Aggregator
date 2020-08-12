namespace KanbanStyle.Domain.Messages
{
    public static class UpdatedInfo
    {
        public static UpdatedInfoFromContinuation<T> From<T>(T oldValue)
            => new UpdatedInfoFromContinuation<T>(oldValue);
    }

    public sealed class UpdatedInfoFromContinuation<T>
    {
        private readonly T _oldValue;

        internal UpdatedInfoFromContinuation(T oldValue)
        {
            _oldValue = oldValue;
        }

        public UpdatedInfo<T> To(T newValue)
            => new UpdatedInfo<T>(_oldValue, newValue);
    }

    public sealed class UpdatedInfo<T>
    {
        public UpdatedInfo()
        {
        }

        internal UpdatedInfo(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; set; }

        public T NewValue { get; set; }

        public bool HasChanged
        {
            get
            {
                if (OldValue == null || NewValue == null)
                {
                    return OldValue != null || NewValue != null;
                }

                return !OldValue.Equals(NewValue);
            }
        }
    }
}
