using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GateKeeper.Authentication
{
    public interface IUserService
    {
        Task<User> Authenticate(string username, string password);
        Task<User> Authenticate(AuthenticationHeaderValue authHeader);
        Task<IEnumerable<User>> GetAll();
    }

    public class UserService : IUserService
    {
        private readonly string _authorizeUrl;
        private readonly string _loginUrl;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public UserService(IConfiguration configuration)
        {
            _authorizeUrl = configuration["BackEndURLs.Authorize"];
            _loginUrl = configuration["BackEndURLs.Login"];
            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<User> Authenticate(string username, string password)
        {
            AuthenticateModel authenticateModel = new AuthenticateModel { Username = username, Password = password };
            var dataString = JsonSerializer.Serialize(authenticateModel);
            HttpStatusCode responseStatusCode;
            string responseBody = "";
            using (var client = new HttpClient())
            {
                using var message = client.PostAsync(_loginUrl, new StringContent(dataString, Encoding.UTF8, "application/json"));
                responseStatusCode = message.Result.StatusCode;
                responseBody = message.Result.Content.ReadAsStringAsync().Result;
            }
            if (responseStatusCode.Equals(HttpStatusCode.OK)) { return JsonSerializer.Deserialize<User>(responseBody, _jsonSerializerOptions); }
            return null;
        }

        public async Task<User> Authenticate(AuthenticationHeaderValue authHeader)
        {
            HttpStatusCode responseStatusCode;
            string responseBody = "";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", authHeader.ToString());
                using var message = client.GetAsync(_authorizeUrl);
                responseStatusCode = message.Result.StatusCode;
                responseBody = message.Result.Content.ReadAsStringAsync().Result;
            }
            if (responseStatusCode.Equals(HttpStatusCode.OK)) { return JsonSerializer.Deserialize<User>(responseBody, _jsonSerializerOptions); }
            return null;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            throw new System.NotImplementedException();
        }
    }
}
