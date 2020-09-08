using BackEnd.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IUserService _userService;

        public LoginController(ILogger<LoginController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpPost]
        // This method enables users to login to the service.
        public IActionResult LoginUserAsync([FromBody] AuthenticateModel model)
        {
            var user = _userService.Authenticate(model.Username, model.Password).Result;
            if (user == null) { return BadRequest("Username or password is incorrect."); }
            return Ok(user);
        }
    }
}
