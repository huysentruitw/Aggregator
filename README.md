# Aggregator

## Usage

### CommandHandler

```C#
class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public Task Handle(CreateUserCommand command)
    {
        // Handle command
    }
}
```

In most cases, command parameters need to be validated before executing the command.

The easiest way to do this is to create a generic base class that uses a validation library like [FluentValidation](https://github.com/JeremySkinner/FluentValidation):

```C#
using FluentValidation;

abstract class SafeCommandHandler<TCommand>
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
```

If a command handler needs to manipulate an aggregate, you'll need some kind of persistent command handler:

```C#
abstract class PersistentCommandHandler<TAggregateRoot, TCommand>
    : SafeCommandHandler<TCommand>
{
    protected readonly IRepository<TAggregateRoot> Repository;

    public PersistentCommandHandler(IRepository<TAggregateRoot> repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
}
```

