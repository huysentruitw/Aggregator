using System.Threading.Tasks;
using AggregatR.Command;
using FluentValidation;

namespace AggregatR.Example.Domain
{
    internal abstract class SafeCommandHandler<TCommand>
        : AbstractValidator<TCommand>
        , ICommandHandler<TCommand>
    {
        public Task Handle(TCommand command)
        {
            DefineRules();
            Validate(command);
            return HandleValidatedCommand(command);
        }

        protected abstract void DefineRules();

        protected abstract Task HandleValidatedCommand(TCommand command);
    }
}
