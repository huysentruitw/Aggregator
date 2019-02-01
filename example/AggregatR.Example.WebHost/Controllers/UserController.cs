using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AggregatR.Command;
using AggregatR.Example.Messages;
using AggregatR.Example.WebHost.Projections;
using Microsoft.AspNetCore.Mvc;

namespace AggregatR.Example.WebHost.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public partial class UserController : Controller
    {
        private readonly ICommandProcessor _commandProcessor;
        private readonly IUserStore _userStore;

        public UserController(ICommandProcessor commandProcessor, IUserStore userStore)
        {
            _commandProcessor = commandProcessor;
            _userStore = userStore;
        }

        [HttpGet("")]
        public IEnumerable<UserModel> All()
        {
            return _userStore
                .GetUsers()
                .Select(x => new UserModel
                {
                    Id = x.Id,
                    GivenName = x.GivenName,
                    Surname = x.Surname,
                    EmailAddress = x.EmailAddress
                });
        }

        [HttpPost("")]
        public async Task<UserModel> PostBody([FromBody] NewUserModel user)
        {
            var id = Guid.NewGuid();

            await _commandProcessor.Process(new CreateUserCommand
            {
                Id = id,
                GivenName = user.GivenName,
                Surname = user.Surname,
                EmailAddress = user.EmailAddress
            }).ConfigureAwait(false);

            return new UserModel(id, user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _commandProcessor.Process(new DeleteUserCommand
            {
                Id = id
            }).ConfigureAwait(false);

            return Ok();
        }

        #region Models

        public class UserModel
        {
            public Guid Id { get; set; }
            public string GivenName { get; set; }
            public string Surname { get; set; }
            public string EmailAddress { get; set; }

            public UserModel() { }

            public UserModel(Guid id, NewUserModel model)
            {
                Id = id;
                GivenName = model.GivenName;
                Surname = model.Surname;
                EmailAddress = model.EmailAddress;
            }
        }

        public class NewUserModel
        {
            public string GivenName { get; set; }
            public string Surname { get; set; }
            public string EmailAddress { get; set; }
        }

        #endregion
    }
}
