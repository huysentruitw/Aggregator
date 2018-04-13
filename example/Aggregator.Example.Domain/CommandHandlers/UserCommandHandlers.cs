using System.Threading.Tasks;
using Aggregator.Example.Domain.Entities;
using Aggregator.Example.Messages;
using Aggregator.Persistence;
using FluentValidation;

namespace Aggregator.Example.Domain.CommandHandlers
{
    internal sealed class CreateUserCommandHandler : PersistentCommandHandler<CreateUserCommand, User>
    {
        public CreateUserCommandHandler(IRepository<User> repository)
            : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.EmailAddress).EmailAddress();
            RuleFor(x => x.GivenName).NotEmpty();
            RuleFor(x => x.Surname).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(CreateUserCommand command)
        {
            AggregateRootId<User> userId = command.Id;
            var user = User.Create(userId, command.EmailAddress, command.GivenName, command.Surname);
            await Repository.Add(userId, user).ConfigureAwait(false);
        }
    }

    internal sealed class UpdateUserEmailAddressCommandHandler : PersistentCommandHandler<UpdateUserEmailAddressCommand, User>
    {
        public UpdateUserEmailAddressCommandHandler(IRepository<User> repository)
            : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.EmailAddress).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(UpdateUserEmailAddressCommand command)
        {
            AggregateRootId<User> userId = command.Id;
            var user = await Repository.Get(userId).ConfigureAwait(false);
            user.SetEmailAddress(command.EmailAddress);
        }
    }

    internal sealed class UpdateUserGivenNameCommandHandler : PersistentCommandHandler<UpdateUserGivenNameCommand, User>
    {
        public UpdateUserGivenNameCommandHandler(IRepository<User> repository)
            : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.GivenName).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(UpdateUserGivenNameCommand command)
        {
            AggregateRootId<User> userId = command.Id;
            var user = await Repository.Get(userId).ConfigureAwait(false);
            user.SetGivenName(command.GivenName);
        }
    }

    internal sealed class UpdateUserSurnameCommandHandler : PersistentCommandHandler<UpdateUserSurnameCommand, User>
    {
        public UpdateUserSurnameCommandHandler(IRepository<User> repository)
            : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Surname).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(UpdateUserSurnameCommand command)
        {
            AggregateRootId<User> userId = command.Id;
            var user = await Repository.Get(userId).ConfigureAwait(false);
            user.SetSurname(command.Surname);
        }
    }

    internal sealed class DeleteUserCommandHandler : PersistentCommandHandler<DeleteUserCommand, User>
    {
        public DeleteUserCommandHandler(IRepository<User> repository)
            : base(repository)
        {
        }

        protected override void DefineRules()
        {
            RuleFor(x => x.Id).NotEmpty();
        }

        protected override async Task HandleValidatedCommand(DeleteUserCommand command)
        {
            AggregateRootId<User> userId = command.Id;
            var user = await Repository.Get(userId).ConfigureAwait(false);
            user.Delete();
        }
    }
}
