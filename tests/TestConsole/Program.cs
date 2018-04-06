using System.Threading.Tasks;
using Aggregator;
using Aggregator.Autofac;
using Aggregator.Command;
using Aggregator.Event;
using Aggregator.Persistence;
using Autofac;

namespace TestConsole
{
    public class AggregatorModule : AggregatorModule<string, object, object>
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<CommandProcessor>().AsSelf().SingleInstance();
        }
    }

    public class CommandProcessor : CommandProcessor<string, object, object>
    {
        public CommandProcessor(ICommandHandlingScopeFactory commandHandlingScopeFactory, IEventDispatcher<object> eventDispatcher, IEventStore<string, object> eventStore)
            : base(commandHandlingScopeFactory, eventDispatcher, eventStore)
        {
        }
    }

    public class EventStore : Aggregator.Persistence.EventStore.EventStore<string, object>
    {
        public EventStore(string connectionString) : base(connectionString)
        {
        }
    }

    public class AggregateRoot : AggregateRoot<string, object>
    {
    }

    class Program
    {
        static IContainer RegisterServices()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<AggregatorModule>();
            builder.RegisterType<EventStore>()
                .AsImplementedInterfaces()
                .WithParameter("connectionString", "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500");

            return builder.Build();
        }

        static void Main(string[] args)
        {
            var container = RegisterServices();

            var processor = container.Resolve<CommandProcessor>();

            processor.Process(new CommandA()).Wait();
        }

        public class CommandA
        {
        }

        public class EventA
        {
        }

        public class CommandHandlerA : ICommandHandler<CommandA>
        {
            private readonly IRepository<string, object, MyAggregateRoot> _repository;

            public CommandHandlerA(IRepository<string, object, MyAggregateRoot> repository)
            {
                _repository = repository;
            }

            public async Task Handle(CommandA command)
            {
                var aggregate = await _repository.Contains("MyAggregate")
                    ? await _repository.Get("MyAggregate")
                    : await _repository.Create("MyAggregate");

                aggregate.DoSomething();
            }
        }

        public class MyAggregateRoot : AggregateRoot
        {
            public MyAggregateRoot()
            {
                Register<EventA>(_ => { });
            }

            public void DoSomething()
            {
                Apply(new EventA());
            }
        }
    }
}
