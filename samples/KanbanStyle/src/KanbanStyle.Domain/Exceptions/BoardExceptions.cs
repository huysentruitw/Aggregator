using System;

namespace KanbanStyle.Domain.Exceptions
{
    public sealed class OperationNotAllowedOnArchivedBoardException : Exception
    {
        public OperationNotAllowedOnArchivedBoardException()
            : base("Operation not allowed on archived board")
        {
        }
    }
}
