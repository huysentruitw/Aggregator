using System.Threading;
using System.Threading.Tasks;
using Aggregator.Persistence;
using FluentValidation;
using KanbanStyle.Domain.Entities;
using KanbanStyle.Domain.Messages;

namespace KanbanStyle.Domain.CommandHandlers
{
    internal sealed class CreateBoardCommandHandler
        : PersistentCommandHandler<CreateBoard, Board>
    {
        private readonly IUtcNowFactory _utcNowFactory;

        public CreateBoardCommandHandler(IRepository<Board> repository, IUtcNowFactory utcNowFactory)
            : base(repository)
        {
            _utcNowFactory = utcNowFactory;
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(CreateBoard command, CancellationToken cancellationToken)
        {
            Id<Board> boardId = command.Id;

            var board = Board.Create(
                id: boardId,
                name: command.Name,
                dateCreatedUtc: _utcNowFactory.UtcNow);

            await Repository.Add(boardId, board);
        }
    }
}
