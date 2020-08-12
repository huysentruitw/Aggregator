using System;

namespace KanbanStyle.Domain.Messages
{
    public sealed class RegisterUser
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }
    }

    public sealed class DeleteUser
    {
        public Guid Id { get; set; }
    }
}
