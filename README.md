# Aggregator

[![Build status](https://ci.appveyor.com/api/projects/status/7uac3qe1fpcl973y/branch/develop?svg=true)](https://ci.appveyor.com/project/huysentruitw/aggregator/branch/develop) [![codecov](https://codecov.io/gh/huysentruitw/Aggregator/branch/develop/graph/badge.svg)](https://codecov.io/gh/huysentruitw/Aggregator) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Aggregator)

Aggregator contains some fundamental base classes and interfaces for building a CQRS/ES based application.

Supports .NET Standard 2.0 and 2.1 (.NET Core 2.2 and 3.1).

## Get it on NuGet

The main package:

    PM> Install-Package Aggregator

EventStore persistence integration package:

    PM> Install-Package Aggregator.Persistence.EventStore

Microsoft DI integration package:

    PM> Install-Package Aggregator.Microsoft.DependencyInjection

Autofac DI integration package:

    PM> Install-Package Aggregator.Autofac

Testing package:

    PM> Install-Package Aggregator.Testing

## Usage

### Registering dependencies

Registering the dependencies in an ASP.NET Core application, using Microsoft.Extensions.DependencyInjection, is pretty simple:
* Install the Aggregator.Microsoft.DependencyInjection package
* Call `app.AddAggregator();` inside the `Configure` method in Startup.cs

If you prefer using Autofac, you can use the Aggregator.Autofac package which contains an Autofac module to ease the registration process.

Need support for a different container? Feel free to [open an issue](https://github.com/huysentruitw/Aggregator/issues/new).

### Aggregate roots

The [`AggregateRoot`](./src/Aggregator/AggregateRoot.cs) or [`AggregateRoot<TEventBase>`](./src/Aggregator/AggregateRoot.cs) class is an abstract base class that should be used as a base for aggregate roots. It allows registering event handlers, initializing the aggregate by replaying events and keeping track of changes getting applied to the aggregate root.

```csharp
sealed class User : AggregateRoot
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
            // ...
        });

    public void SetGivenName(string givenName)
    {
        if (_givenName.Equals(givenName)) return;

        Apply(new UpdatedUserGivenNameEvent
        {
            GivenName = UpdatedInfo.From(_givenName).To(givenName),
            Surname = _surname,
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

The generic [`Repository<TAggregateRoot>`](./src/Aggregator/Persistence/Repository.cs) class is responsible for creating new aggregate roots, loading aggregate roots from the event store and keeping track of changes applied to new or loaded aggregate roots.

This generic repository is registered as a scoped instance (when using one of the DI integration packages) as an instance should only exist for the lifetime of a single command being processed.

### Commands and events

Commands and events can be defined as simple POCO's. All Aggregator related classes have a generic variant which allows you to define a base type for commands and events for those who like type safety.

Example command:

```csharp
class CreateUserCommand
{
    public string Id { get; set; }
    public string EmailAddress { get; set; }
    public string GivenName { get; set; }
    public string Surname { get; set; }
}
```

Example event:

```csharp
class UserCreatedEvent
{
    public string Id { get; set; }
    public string EmailAddress { get; set; }
    public string GivenName { get; set; }
    public string Surname { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
```

### Command handlers

#### Definition

The library contains a generic interface definition [`ICommandHandler<TCommand>`](./src/Aggregator.Abstractions/Command/ICommandHandler.cs) that identifies command handlers.

```csharp
class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public override Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Process the command...
    }
}
```

#### Registering command handlers

Command handlers can be registered one-by-one or in one go using [Scrutor](https://github.com/khellang/Scrutor) for Microsoft.Extensions.DependencyInjection or `AsClosedTypesOf` when using Autofac.

#### Add command validation to the command handler

It's a good practice to validate commands before executing them. A great way to do this is to create a base class that uses [FluentValidation](https://github.com/JeremySkinner/FluentValidation).

```csharp
abstract class SafeCommandHandler<TCommand>
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
```

#### Using aggregate roots in a command handler

Since most command handlers will manipulate an aggregate root, you can also create a base class that requires a repository.

```csharp
abstract class PersistentCommandHandler<TCommand, TAggregateRoot>
    : SafeCommandHandler<TCommand>
    where TAggregateRoot : AggregateRoot, new()
{
    protected readonly IRepository<TAggregateRoot> Repository;

    protected PersistentCommandHandler(IRepository<TAggregateRoot> repository)
    {
        Repository = repository;
    }
}
```

Usage:

```csharp
sealed class UpdateTodoTitleCommandHandler
    : PersistentCommandHandler<UpdateTodoTitleCommand, TodoAggregateRoot>
{
    public UpdateTodoTitleCommandHandler(IRepository<TodoAggregateRoot> repository)
        : base(repository)
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
```

### CommandProcessor

The [`CommandProcessor`](./src/Aggregator/Command/CommandProcessor.cs) or [`CommandProcessor<TIdentifier, TCommandBase, TEventBase>`](./src/Aggregator/Command/CommandProcessor.cs) class is responsible for processing commands, which consists of these steps:

* Requests a new `CommandHandlingContext` from the DI container which will maintain the internal unit-of-work
* Execute one or more command handlers that implement the `ICommandHandler<TCommand>` interface
* Store events that were tracked by the unit-of-work
* Dispatch events that were tracked by the unit-of-work in the `CommandHandlingContext`

### IServiceScopeFactory / IServiceScope

The implementation of the [`IServiceScopeFactory`](./src/Aggregator/DI/IServiceScopeFactory.cs) interface is responsible for creating a temporary child scope from which the `CommandHandlingContext`, command handlers and event handlers are resolved.

The child scope, which implements [`IServiceScope`](./src/Aggregator/DI/IServiceScope.cs) is only valid for the lifetime of a single command or event being processed/dispatched.

These dedicated interfaces are DI independent. There's an integration NuGet package available for Microsoft.Extensions.DI (`Aggregator.Microsoft.DependencyInjection`) and Autofac (`Aggregator.Autofac`) that can be used out of the box or serve as an example.

### CommandHandlingContext

The [`CommandHandlingContext`](./src/Aggregator/Command/CommandHandlingContext.cs) is resolved from the `IServiceScope` instance by the `CommandProcessor` for the lifetime of one single command being processed. It's a property bag that can be used to store and retrieve properties during the processing of a single command. Internally, this context is also used to store the unit-of-work that is required by the `Repository` class to keep track of changes (events) generated by aggregate root entities.

### Store events

The `CommandProcessor` depends on an implementation of [`IEventStore<TIdentifier, TEventBase>`](./src/Aggregator/Persistence/IEventStore.cs) which will be used to store one or more events that were generated during the processing of a single command in a transactional manner. When something goes wrong while storing (and dispatching) the event(s), the complete transaction will get rolled back and the exception will bubble up to the caller.

There's an integration NuGet package available for using EventStore (`Aggregator.Persistence.EventStore`) that can be used or serve as an example for a custom event store.

### Dispatch events

The `CommandProcessor` also depends on an implementation of [`IEventDispatcher<TEventBase>`](./src/Aggregator/Event/IEventDispatcher.cs) which will be used to dispatch one or more events that were generated during the processing of a single command inside the command domain. The implementation of the `IEventDispatcher<TEventBase>` interface is responsible for forwarding events to classes that implement the [`IEventHandler<TEvent>`](./src/Aggregator/Event/IEventHandler.cs) interface inside the command domain. A typical example of classes that listen to one or more events are Process Managers (sometimes referred to as Sagas) that act on events, keep track of some kind of long running state and send out commands depending on that state. Since the work and state of process managers is important, the `CommandProcessor` will also rollback the event store transaction in case something goes wrong during event dispatching.

[`EventDispatcher<TEventBase>`](./src/Aggregator/Event/EventDispatcher.cs) is a default implementation that can be used for dispatching events.

## Testing Aggregates

The package Aggregator.Testing can be used to write unit-tests against your aggregate roots.

### Testing static constructors

```csharp
Scenario
    .ForConstructor(() => User.Create("John", "Doe"))
    .Then(new CreatedUserEvent
    {
        GivenName = "John",
        Surname = "Doe",
    })
    .Assert();
```

### Testing aggregate commands

```csharp
Scenario
    .ForCommand(User.Factory)
    .Given(
        new CreatedUserEvent
        {
            GivenName = "John",
            Surname = "Doe",
        })
    .When(user => user.SetGivenName("Jon"))
    .Then(
        new UpdatedUserGivenNameEvent
        {
            GivenName = UpdatedInfo.From("John").To("Jon"),
            Surname = "Doe",
        })
    .Assert();
```

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for detail
