using System.Threading;
using System.Threading.Tasks;
using Aggregator;
using FluentValidation;

namespace KanbanStyle.Domain.CommandHandlers
{
    internal abstract class SafeCommandHandler<TCommand>
        : AbstractValidator<TCommand>
        , ICommandHandler<TCommand>
    {
        public async Task Handle(TCommand command, CancellationToken cancellationToken)
        {
            DefineRules();
            await this.ValidateAndThrowAsync(command, cancellationToken: cancellationToken);
            await HandleValidatedCommand(command, cancellationToken);
        }

        protected abstract void DefineRules();

        protected abstract Task HandleValidatedCommand(TCommand command, CancellationToken cancellationToken);
    }
}
