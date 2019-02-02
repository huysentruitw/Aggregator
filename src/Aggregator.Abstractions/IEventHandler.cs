namespace Aggregator
{
    /// <summary>
    /// Interface for a class is able to handle an event of the given type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    public interface IEventHandler<TEvent> : MediatR.INotificationHandler<TEvent>
        where TEvent : IEvent
    {
    }
}
