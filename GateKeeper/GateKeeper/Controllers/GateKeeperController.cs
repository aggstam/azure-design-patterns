using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using GateKeeper.Authentication;
using GateKeeper.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GateKeeper.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GateKeeperController : ControllerBase
    {
        private readonly ILogger<GateKeeperController> _logger;
        private readonly IUserService _userService;
        private readonly IValidationService _validationService;
        private readonly string _signUpUrl;
        private readonly string _filesUrl;
        private readonly string _refreshValetKeyUrl;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public GateKeeperController(IConfiguration configuration, ILogger<GateKeeperController> logger, IUserService userService, IValidationService validationService)
        {
            _logger = logger;
            _userService = userService;
            _validationService = validationService;
            _signUpUrl = configuration["BackEndURLs.SignUp"];
            _filesUrl = configuration["BackEndURLs.Files"];
            _refreshValetKeyUrl = configuration["BackEndURLs.Files.RefreshValetKey"];
            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult LoginUser([FromBody] AuthenticateModel model)
        {
            try
            {
                List<string> validationErrors = _validationService.ValidateCredentials(model.Username, model.Password);
                if (validationErrors.Count > 0) return BadRequest(validationErrors);
                var user = _userService.Authenticate(model.Username, model.Password).Result;
                if (user == null) { return BadRequest("Username or password is incorrect."); }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("[GateKeeperController/LoginUser] Exception occured. Message: {0}", ex.Message);
                return Ok(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public IActionResult SignUpUser([FromBody] User user)
        {
            try
            {
                List<string> validationErrors = _validationService.ValidateCredentials(user.Username, user.Password);
                if (validationErrors.Count > 0) return BadRequest(validationErrors);
                var userString = JsonSerializer.Serialize(user);                
                HttpStatusCode responseStatusCode;
                string responseBody = "";
                using (var client = new HttpClient())
                {
                    using var message = client.PostAsync(_signUpUrl, new StringContent(userString, Encoding.UTF8, "application/json"));
                    responseStatusCode = message.Result.StatusCode;
                    responseBody = message.Result.Content.ReadAsStringAsync().Result;
                }
                if (responseStatusCode.Equals(HttpStatusCode.OK))
                {
                    user = JsonSerializer.Deserialize<User>(responseBody, _jsonSerializerOptions);
                    return Ok(user);
                }
                else if (responseStatusCode.Equals(HttpStatusCode.BadRequest)) { return BadRequest("User already exists."); }
                return Ok(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("[GateKeeperController/SignUpUser] Exception occured. Message: {0}", ex.Message);
                return Ok(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("files/{username}")]
        public IActionResult GetUserFilesInfo([FromRoute] string username)
        {
            try
            {
                if (!_validationService.ValidateCaller(Request.Headers["Authorization"], username)) return Unauthorized();
                string filesUrl = string.Format("{0}/{1}", _filesUrl, username);
                HttpStatusCode responseStatusCode;
                string responseBody = "";
                using (var client = new HttpClient())
                {
                    using var message = client.GetAsync(filesUrl);
                    responseStatusCode = message.Result.StatusCode;
                    responseBody = message.Result.Content.ReadAsStringAsync().Result;
                }
                if (responseStatusCode.Equals(HttpStatusCode.OK)) { return Ok(JsonSerializer.Deserialize<List<FileInfo>>(responseBody, _jsonSerializerOptions)); }
                return Ok(responseStatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("[GateKeeperController/GetUserFilesInfo] Exception occured. Message: {0}", ex.Message);
                return Ok(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("files/refreshValetKey/{username}/{fileName}")]
        public IActionResult RefreshUserFileValetKey([FromRoute] string username, string fileName)
        {
            try
            {
                if (!_validationService.ValidateCaller(Request.Headers["Authorization"], username)) return Unauthorized();
                string lifeTime = Request.Query["lifeTime"];
                string validationError = _validationService.ValidateValetKeyLifeTime(lifeTime);
                if (validationError != null) { return BadRequest(validationError); }
                string refreshValetKeyUrl = string.Format("{0}/{1}/{2}?lifeTime={3}", _refreshValetKeyUrl, username, fileName, lifeTime);
                HttpStatusCode responseStatusCode;
                string responseBody = "";
                using (var client = new HttpClient())
                {
                    using var message = client.GetAsync(refreshValetKeyUrl);
                    responseStatusCode = message.Result.StatusCode;
                    responseBody = message.Result.Content.ReadAsStringAsync().Result;
                }
                if (responseStatusCode.Equals(HttpStatusCode.OK)) { return Ok(JsonSerializer.Deserialize<FileInfo>(responseBody, _jsonSerializerOptions)); }
                return Ok(responseStatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("[GateKeeperController/RefreshUserFileValetKey] Exception occured. Message: {0}", ex.Message);
                return Ok(ex.Message);
            }            
        }

        [Authorize]
        [HttpDelete("files/{username}/{fileName}")]
        public IActionResult DeleteUserFile([FromRoute] string username, string fileName)
        {
            try
            {
                if (!_validationService.ValidateCaller(Request.Headers["Authorization"], username)) return Unauthorized();
                string backendUrl = string.Format("{0}/{1}/{2}", _filesUrl, username, fileName);
                HttpStatusCode responseStatusCode;
                string responseBody = "";
                using (var client = new HttpClient())
                {
                    using var message = client.DeleteAsync(backendUrl);
                    responseStatusCode = message.Result.StatusCode;
                    responseBody = message.Result.Content.ReadAsStringAsync().Result;
                }
                return Ok(responseStatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("[GateKeeperController/DeleteUserFile] Exception occured. Message: {0}", ex.Message);
                return Ok(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("files/{username}")]
        public IActionResult PostUserFile([FromRoute] string username, [FromForm] IFormFile file)
        {
            try
            {
                if (!_validationService.ValidateCaller(Request.Headers["Authorization"], username)) return Unauthorized();
                string validationError = _validationService.ValidateFile(file);
                if (validationError != null) { return BadRequest(validationError); }
                string backendUrl = string.Format("{0}/{1}", _filesUrl, username);
                HttpStatusCode responseStatusCode;
                string responseBody = "";
                using (var client = new HttpClient())
                {
                    using var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture))
                    {
                        { new StreamContent(file.OpenReadStream()), "file", file.FileName }
                    };
                    using var message = client.PostAsync(backendUrl, content);
                    responseStatusCode = message.Result.StatusCode;
                    responseBody = message.Result.Content.ReadAsStringAsync().Result;
                }
                return Ok(responseStatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("[GateKeeperController/PostUserFile] Exception occured. Message: {0}", ex.Message);
                return Ok(ex.Message);
            }
        }

        public class FileInfo
        {
            public string FileName { get; set; }
            public string FileUrl { get; set; }
        }
    }
}
