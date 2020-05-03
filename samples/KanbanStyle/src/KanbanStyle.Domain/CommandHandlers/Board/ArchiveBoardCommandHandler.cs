using System.Threading;
using System.Threading.Tasks;
using Aggregator.Persistence;
using FluentValidation;
using KanbanStyle.Domain.Entities;
using KanbanStyle.Domain.Identifiers;
using KanbanStyle.Domain.Messages;

namespace KanbanStyle.Domain.CommandHandlers
{
    internal sealed class ArchiveBoardCommandHandler
        : PersistentCommandHandler<ArchiveBoard, Board>
    {
        private readonly IUtcNowFactory _utcNowFactory;

        public ArchiveBoardCommandHandler(IRepository<Board> repository, IUtcNowFactory utcNowFactory)
            : base(repository)
        {
            _utcNowFactory = utcNowFactory;
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(ArchiveBoard command, CancellationToken cancellationToken)
        {
            Id<Board> id = command.Id;

            Board board = await Repository.Get(id);

            board.Archive(dateArchivedUtc: _utcNowFactory.UtcNow);
        }
    }
}
