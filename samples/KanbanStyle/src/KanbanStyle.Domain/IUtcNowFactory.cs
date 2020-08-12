using System;

namespace KanbanStyle.Domain
{
    internal interface IUtcNowFactory
    {
        DateTime UtcNow { get; }
    }
}
