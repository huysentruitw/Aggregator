using System.Threading.Tasks;

namespace AggregatR.Example.WebHost.Projections.Infrastructure
{
    internal interface IProjection
    {
        Task Handle(object @event);
    }
}
