using System.Threading;
using System.Threading.Tasks;
using Aggregator.Persistence;
using FluentValidation;
using KanbanStyle.Domain.Entities;
using KanbanStyle.Domain.Identifiers;
using KanbanStyle.Domain.Messages;

namespace KanbanStyle.Domain.CommandHandlers
{
    internal sealed class UpdateBoardNameCommandHandler
        : PersistentCommandHandler<UpdateBoardName, Board>
    {
        private readonly IUtcNowFactory _utcNowFactory;

        public UpdateBoardNameCommandHandler(IRepository<Board> repository, IUtcNowFactory utcNowFactory)
            : base(repository)
        {
            _utcNowFactory = utcNowFactory;
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.NewName).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(UpdateBoardName command, CancellationToken cancellationToken)
        {
            Id<Board> boardId = command.Id;

            Board board = await Repository.Get(boardId);

            board.UpdateName(
                newName: command.NewName,
                dateUpdatedUtc: _utcNowFactory.UtcNow);
        }
    }
}
