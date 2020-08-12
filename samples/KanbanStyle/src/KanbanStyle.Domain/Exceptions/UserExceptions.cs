using System;

namespace KanbanStyle.Domain.Exceptions
{
    public sealed class OperationNotAllowedOnDeletedUserException : Exception
    {
        public OperationNotAllowedOnDeletedUserException()
            : base("Operation not allowed on deleted user")
        {
        }
    }
}
