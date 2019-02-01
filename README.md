# AggregatR

[![Build status](https://ci.appveyor.com/api/projects/status/c53dm2n5vcguo3e8/branch/master?svg=true)](https://ci.appveyor.com/project/huysentruitw/aggregator/branch/master)

This package contains some fundamental base classes and interfaces for building a CQRS/ES based application. Supports .NET Core / .NET Standard / .NET Framework.

## Get it on NuGet

The main package:

    PM> Install-Package Aggregator

EventStore persistence integration package:

    PM> Install-Package Aggregator.Persistence.EventStore

Microsoft DI integration package:

    PM> Install-Package Aggregator.Microsoft.DependencyInjection

Autofac DI integration package:

    PM> Install-Package Aggregator.Autofac

## Usage

### Generic interfaces and base types

For flexibility, this library consists of generic base types and generic interfaces that lets you define your own base type for aggregate root identifiers (`TIdentifier`), commands (`TCommandBase`) and events (`TEventBase`). Most interfaces or base types have overloads that use `string` as aggregate root identifier type, and `object` as event and command base type which you can use in case your application doesn't have complex requirements.

### AggregateRoot

The [`AggregateRoot`](./src/AggregatR/AggregateRoot.cs) or [`AggregateRoot<TEventBase>`](./src/AggregatR/AggregateRoot.cs) class is an abstract base class that should be used as a base for aggregate roots. It allows registering event handlers, initializing the aggregate by replaying events and keeping track of changes getting applied to the aggregate root.

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

The generic [`Repository<TAggregateRoot>`](./src/AggregatR/Persistence/Repository.cs) class is responsible for creating new aggregate roots, loading aggregate roots from the event store and keeping track of changes applied to new or loaded aggregate roots.

This generic repository is registered as a scoped instance (when using one of the DI integration packages) as an instance should only exist for the lifetime of a single command being processed.

### Command handlers

The library contains a generic interface definition [`ICommandHandler<TCommand>`](./src/AggregatR/Command/ICommandHandler.cs) that identifies command handlers.

For example:

```csharp
class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public Task Handle(CreateUserCommand command)
    {
        // Handle command
    }
}
```

A great way to validate commands in the command handler is to use a validation library like [FluentValidation](https://github.com/JeremySkinner/FluentValidation):

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

Additionally, if a command handler needs to manipulate an aggregate, you could create some kind of persistent command handler:

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

The [`CommandProcessor`](./src/AggregatR/Command/CommandProcessor.cs) or [`CommandProcessor<TIdentifier, TCommandBase, TEventBase>`](./src/AggregatR/Command/CommandProcessor.cs) class is responsible for processing commands, which consists of these steps:

* Requests a new `CommandHandlingContext` from the DI container which will maintain the internal unit-of-work
* Execute one or more command handlers that implement the `ICommandHandler<TCommand>` interface
* Store events that were tracked by the unit-of-work
* Dispatch events that were tracked by the unit-of-work in the `CommandHandlingContext`

### IServiceScopeFactory / IServiceScope

The implementation of the [`IServiceScopeFactory`](./src/AggregatR/DI/IServiceScopeFactory.cs) interface is responsible for creating a temporary child scope from which the `CommandHandlingContext`, command handlers and event handlers are resolved.

The child scope, which implements [`IServiceScope`](./src/AggregatR/DI/IServiceScope.cs) is only valid for the lifetime of a single command or event being processed/dispatched.

These dedicated interfaces are DI independent. There's an integration NuGet package available for Microsoft.Extensions.DI (`AggregatR.Microsoft.DependencyInjection`) and Autofac (`AggregatR.Autofac`) that can be used out of the box or serve as an example.

### CommandHandlingContext

The [`CommandHandlingContext`](./src/AggregatR/Command/CommandHandlingContext.cs) is resolved from the `IServiceScope` instance by the `CommandProcessor` for the lifetime of one single command being processed. It's a property bag that can be used to store and retrieve properties during the processing of a single command. Internally, this context is also used to store the unit-of-work that is required by the `Repository` class to keep track of changes (events) generated by aggregate root entities.

### Store events

The `CommandProcessor` depends on an implementation of [`IEventStore<TIdentifier, TEventBase>`](./src/AggregatR/Persistence/IEventStore.cs) which will be used to store one or more events that were generated during the processing of a single command in a transactional manner. When something goes wrong while storing (and dispatching) the event(s), the complete transaction will get rolled back and the exception will bubble up to the caller.

There's an integration NuGet package available for using EventStore (`AggregatR.Persistence.EventStore`) that can be used or serve as an example for a custom event store.

### Dispatch events

The `CommandProcessor` also depends on an implementation of [`IEventDispatcher<TEventBase>`](./src/AggregatR/Event/IEventDispatcher.cs) which will be used to dispatch one or more events that were generated during the processing of a single command inside the command domain. The implementation of the `IEventDispatcher<TEventBase>` interface is responsible for forwarding events to classes that implement the [`IEventHandler<TEvent>`](./src/AggregatR/Event/IEventHandler.cs) interface inside the command domain. A typical example of classes that listen to one or more events are Process Managers (sometimes referred to as Sagas) that act on events, keep track of some kind of long running state and send out commands depending on that state. Since the work and state of process managers is important, the `CommandProcessor` will also rollback the event store transaction in case something goes wrong during event dispatching.

[`EventDispatcher<TEventBase>`](./src/AggregatR/Event/EventDispatcher.cs) is a default implementation that can be used for dispatching events.

## The example

A very basic example is included in this solution that demonstrates the usage of the AggregatR library in combination with EventStore.

The example consists of 3 separate projects, although a real-life implementation should probably have more layers.

### AggregatR.Example.Messages project

The messages project contains all base message types like commands and events which are typically shared between both sides in a CQRS/ES application.

### AggregatR.Example.Domain project

This project contains the domain part of the application. The domain part contains the command handlers and the aggregate root entities that generate events.

Generated events are published to streams using [EventStore](https://eventstore.org/), each aggregate root has its own stream in the event store.

In a real-life implementation, the domain part should also have some kind of API layer or command bus over which we can request the execution of certain commands.

In this example, we will use the `CommandProcessor` directly from our WebHost project. To facilitate this, we had to add a public `Dummy` class to the domain project and load the domain assembly in our WebHost `AppDomain`.

### AggregatR.Example.WebHost project

This is a ASP.NET Core Web API project that also hosts a single-page Angular website.

It also contains some simple in memory projections that gets rebuilt from the event store each time the application starts. The `EventStoreProjector` class will generate and keep our projections up-to-date.

### EventStore

Just [download](https://eventstore.org/downloads/) and run EventStore with default settings. The connection string in `appsettings.json` will connect with an instance running locally.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/huysentruitw/AggregatR/tags). 

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for detail
