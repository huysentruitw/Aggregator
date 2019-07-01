using System.Threading;
using System.Threading.Tasks;
using Aggregator.Example.Domain.Entities;
using Aggregator.Persistence;
using FluentValidation;

namespace Aggregator.Example.Domain.CommandHandlers
{
    public sealed class AddTodoCommandHandler : PersistentCommandHandler<AddTodoCommand, TodoAggregateRoot>
    {
        public AddTodoCommandHandler(IRepository<TodoAggregateRoot> repository) : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(AddTodoCommand command, CancellationToken cancellationToken)
        {
            var aggregateRoot = TodoAggregateRoot.Create(
                id: command.Id.ToString(),
                title: command.Title,
                description: command.Description,
                dueDate: command.DueDate);

            await Repository.Add(command.Id.ToString(), aggregateRoot);
        }
    }

    public sealed class RemoveTodoCommandHandler : PersistentCommandHandler<RemoveTodoCommand, TodoAggregateRoot>
    {
        public RemoveTodoCommandHandler(IRepository<TodoAggregateRoot> repository) : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(RemoveTodoCommand command, CancellationToken cancellationToken)
        {
            var aggregateRoot = await Repository.Get(command.Id.ToString());
            aggregateRoot.Remove();
        }
    }

    public sealed class UpdateTodoTitleCommandHandler : PersistentCommandHandler<UpdateTodoTitleCommand, TodoAggregateRoot>
    {
        public UpdateTodoTitleCommandHandler(IRepository<TodoAggregateRoot> repository) : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(UpdateTodoTitleCommand command, CancellationToken cancellationToken)
        {
            var aggregateRoot = await Repository.Get(command.Id.ToString());
            aggregateRoot.UpdateTitle(command.Title);
        }
    }

    public sealed class UpdateTodoDescriptionCommandHandler : PersistentCommandHandler<UpdateTodoDescriptionCommand, TodoAggregateRoot>
    {
        public UpdateTodoDescriptionCommandHandler(IRepository<TodoAggregateRoot> repository) : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(UpdateTodoDescriptionCommand command, CancellationToken cancellationToken)
        {
            var aggregateRoot = await Repository.Get(command.Id.ToString());
            aggregateRoot.UpdateDescription(command.Description);
        }
    }

    public sealed class UpdateTodoDueDateCommandHandler : PersistentCommandHandler<UpdateTodoDueDateCommand, TodoAggregateRoot>
    {
        public UpdateTodoDueDateCommandHandler(IRepository<TodoAggregateRoot> repository) : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(UpdateTodoDueDateCommand command, CancellationToken cancellationToken)
        {
            var aggregateRoot = await Repository.Get(command.Id.ToString());
            aggregateRoot.UpdateDueDate(command.DueDate);
        }
    }
}
