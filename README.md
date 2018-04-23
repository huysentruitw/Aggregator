# Aggregator

This package contains some fundamental base classes and interfaces for building a CQRS/ES based application.

## Get it on NuGet

The main package:

    PM> Install-Package Aggregator

Autofac integration package:

    PM> Install-Package Aggregator.Autofac

EventStore persistence integration package:

    PM> Install-Package Aggregator.Persistence.EventStore

## Usage

### Generic interfaces and base types

For flexibility, this library consists of generic base types and generic interfaces that let you define your own base type for aggregate root identifiers (`TIdentifier`), commands (`TCommandBase`) and events (`TEventBase`). Most interfaces or base types have overloads that use `string` as aggregate root identifier type, and `object` as event and command base type which you can use if you don't have complex requirements.

### AggregateRoot

The `AggregateRoot` or `AggregateRoot<TEventBase>` class is an abstract base class that should be used as a base for aggregate roots. It allows registering event handlers, initializing the aggregate by replaying events and keeping track of changes getting applied to the aggregate root.

```csharp
class User : AggregateRoot
{
    private string _givenName;
    private string _surName;

    public User()
    {
        Register<CreatedUserEvent>(OnCreated);
        Register<UpdatedUserGivenNameEvent>(OnGivenNameUpdated);
        // ...
    }

    // When having multiple constructors, always call the one that registers the event handlers
    private User(CreatedUserEvent @event) : this()
    {
        Apply(@event);
    }

    public static User Create(string givenName, string surname)
        => new User(new CreatedUserEvent
        {
            GivenName = givenName,
            Surname = surname,
            DateCreatedUtc = DateTimeOffset.UtcNow,
            // ...
        });

    public void SetGivenName(string givenName)
    {
        if (_givenName.Equals(givenName)) return;

        Apply(new UpdatedUserGivenNameEvent
        {
            GivenName = UpdatedInfo.From(_givenName).To(givenName),
            Surname = _surname,
            DateUpdatedUtc = DateTimeOffset.UtcNow,
            // ...
        });
    }

    private void OnCreated(CreatedUserEvent @event)
    {
        _givenName = @event.GivenName;
        _surname = @event.Surname;
    }

    private void OnGivenNameUpdated(UpdatedUserGivenNameEvent @event)
    {
        _givenName = @event.GivenName.NewValue;
    }

    // ...
}
```

### Repository

The generic `Repository<TAggregateRoot>` class is responsible for creating new aggregate roots, loading aggregate roots from the event store and keeping track of changes applied to new or loaded aggregate roots.

A scoped instance of this generic class needs to be injected into the command handlers that needs to create or modify an aggregate root. To allow unit-testing of the command handlers, the `IRepository<TAggregateRoot>` or `IRepositoryTIdentifier, TEventBase, TAggregateRoot>` should be injected instead.

A repository is scoped and should only exist for the lifetime of a single command being processed. Because the repository needs to have access to the unit-of-work in the `CommandHandlingContext`, `Repository` instances are resolved from a childscope created by the `ICommandHandlingScopeFactory` implementation (see below).

### Command handlers

The library contains a generic interface definition `ICommandHandler<TCommand>` that identifies command handlers.

```csharp
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

```csharp
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

```csharp
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

### CommandProcessor

The `CommandProcessor` or `CommandProcessor<TIdentifier, TCommandBase, TEventBase>` class is responsible for processing commands, which consists of these steps:

* Create a `CommandHandlingContext` which maintains the internal unit-of-work
* Execute one or more command handlers that implement the `ICommandHandler<TCommand>` interface
* Store events that were tracked by the unit-of-work
* Dispatch events that were tracked by the unit-of-work in the `CommandHandlingContext`

Since each call to `Process` creates a new `CommandHandlingContext`, we need to implement some kind of dependency injection that is responsible for injecting this context into newly resolved command handler instances. The class that implements the `ICommandHandlingScopeFactory` and `ICommandHandlingScope` is responsible for resolving scoped instances of command handlers that match the command to be processed. The lifetime of these command handler instances only span the execution of a single command.

Since the `CommandProcessor` keeps an internal cache of generic methods, it is wise to only have one single instance of this class that is used through the entire lifetime of the application.

### CommandHandlingContext

The `CommandHandlingContext` is created by the `CommandProcessor` for the lifetime of one single command being processed. It's a property bag that can be used to store and retrieve properties during the processing of a single command. Internally, this context is also used to store the unit-of-work that is required by the `Repository` class to keep track of changes (events) generated by aggregate root entities.

### ICommandHandlingScopeFactory / ICommandHandlingScope

The implementation of the `ICommandHandlingScopeFactory` interface is responsible for creating a temporary child scope from which the command handlers for a certain command type can be resolved. The child scope should also have the `CommandHandlingContext` registered so the `Repository` class used by the command handler can also be resolved (as the `Repository` class depends on the `CommandHandlingContext` for its unit-of-work).

The child scope, which implements `ICommandHandlingScope<TCommand>` is only valid for the lifetime of a single command being processed.

These dedicated interfaces are DI independent. There's an integration NuGet package available for Autofac (`Aggregator.Autofac`) that can be used out of the box or serve as an example. Using the `Microsoft.Extensions.DependencyInjection` currently doesn't work as that implementation does not allow the creation of childscopes while registering additional services.

### Store events

The `CommandProcessor` depends on an implementation of `IEventStore<TIdentifier, TEventBase>` which will be used to store one or more events that were generated during the processing of a single command in a transactional manner. When something goes wrong while storing (and dispatching) the event(s), the complete transaction will get rolled back and the exception will bubble up to the caller.

### Dispatch events

The `CommandProcessor` also depends on an implementation of `IEventDispatcher<TEventBase>` which will be used to dispatch one or more events that were generated during the processing of a single command inside the command domain. The implementation of the `IEventDispatcher<TEventBase>` interface is responsible for forwarding events to classes that implement the `IEventHandler<TEvent>` interface inside the command domain. A typical example of classes that listen to one or more events are Process Managers (sometimes referred to as Sagas) that act on events, keep track of some kind of long running state and send out commands depending on that state. Since the work and state of process managers is important, the `CommandProcessor` will also rollback the event store transaction in case something goes wrong during event dispatching.

## The example

A very basic example is included in this solution that demonstrates the usage of the Aggregator library in combination with EventStore.

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

### EventStore

Just [download](https://eventstore.org/downloads/) and run EventStore with default settings. The connection string in `appsettings.json` will connect with an instance running locally.
