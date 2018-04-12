using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.Example.WebHost.Domain.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Aggregator.Example.WebHost.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public partial class UserController : Controller
    {
        private readonly ICommandProcessor _commandProcessor;

        public UserController(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        [HttpGet("")]
        public IEnumerable<UserModel> All()
        {
            return new[]
            {
                new UserModel { Id = Guid.NewGuid(), GivenName = "James", Surname = "Knoll", EmailAddress = "james.knoll@gmail.com" },
                new UserModel { Id = Guid.Parse("bfdf57be-5a32-496d-b5fc-37c08b3899de"), GivenName = "Kurt", Surname = "Wally", EmailAddress = "kurt.wally@gmail.com" }
            };
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
