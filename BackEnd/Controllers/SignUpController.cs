using BackEnd.Authentication;
using BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SignUpController : ControllerBase
    {
        private readonly ILogger<SignUpController> _logger;
        private readonly IUserService _userService;

        public SignUpController(ILogger<SignUpController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpPost]
        // This method enables users to signup to the service.
        public IActionResult SignUpUser([FromBody] User user)
        {
            var created = _userService.CreateUser(user).Result;
            if (created) { return Ok(user); }
            return BadRequest("User already exists.");
        }
    }
}
