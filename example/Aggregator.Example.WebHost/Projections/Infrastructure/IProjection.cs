using System.Threading.Tasks;

namespace Aggregator.Example.WebHost.Projections.Infrastructure
{
    internal interface IProjection
    {
        Task Handle(object @event);
    }
}
