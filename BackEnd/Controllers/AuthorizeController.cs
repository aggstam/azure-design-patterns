using System;
using System.Net.Http.Headers;
using System.Text;
using BackEnd.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthorizeController : ControllerBase
    {
        private readonly ILogger<AuthorizeController> _logger;
        private readonly IUserService _userService;

        public AuthorizeController(ILogger<AuthorizeController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpGet]
        // This method is used to check authorization header validity.
        public IActionResult AuthorizeAsync()
        {
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            var username = credentials[0];
            var password = credentials[1];
            var user = _userService.Authenticate(username, password).Result;
            if (user == null) { return BadRequest("Username or password is incorrect."); }
            return Ok(user);
        }
    }
}
