using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace Aggregator.Example.Domain.CommandHandlers
{
    public abstract class SafeCommandHandler<TCommand>
        : AbstractValidator<TCommand>
        , ICommandHandler<TCommand>
    {
        public async Task Handle(TCommand command, CancellationToken cancellationToken)
        {
            DefineRules();
            this.ValidateAndThrow(command);
            await HandleValidatedCommand(command, cancellationToken).ConfigureAwait(false);
        }

        protected abstract void DefineRules();

        protected abstract Task HandleValidatedCommand(TCommand command, CancellationToken cancellationToken);
    }
}
