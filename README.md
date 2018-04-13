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
abstract class PersistentCommandHandler<TCommand, TAggregateRoot>
    : SafeCommandHandler<TCommand>
    where TAggregateRoot : AggregateRoot, new()
{
    protected readonly IRepository<TAggregateRoot> Repository;

    public PersistentCommandHandler(IRepository<TAggregateRoot> repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
}
```

... wip ...

## Running the example

A very basic example is included in this solution that demonstrates the usage of the Aggregator library.

The example consists of 3 separate projects, although a real-life implementation should probably have more layers.

### Aggregator.Example.Messages project

The messages project contains all base message types like commands and events which are typically shared between both sides in a CQRS/ES application.

### Aggregator.Example.Domain project

This project contains the domain part of the application. The domain part contains the command handlers and the aggregate root entities that generate events.

Generated events are published to streams using [EventStore](https://eventstore.org/), each aggregate root has its own stream in the event store.

In a real-life implementation, the domain part should also have some kind of API layer or command bus over which we can request the execution of certain commands.

In this example, we will use the `CommandProcessor` directly from our WebHost project. To facilitate this, we had to add a public `Dummy` class to the domain project and load the domain assembly in our WebHost `AppDomain`.

### Aggregator.Example.WebHost project

This is a ASP.NET Core Web API project that also hosts a single-page Angular website.

It also contains some simple in memory projections that gets rebuilt from the event store each time the application starts. The `EventStoreProjector` class will generate and keep our projections up-to-date.



