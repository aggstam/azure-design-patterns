using Microsoft.Extensions.Configuration;
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
    }

    public class UserService : IUserService
    {
        private readonly string _authorizeUrl; // Backend authorize controller endpoint.
        private readonly string _loginUrl; // Backend login controller endpoint.
        private readonly JsonSerializerOptions _jsonSerializerOptions;  // Used to remove case sensitivity of serializer.

        public UserService(IConfiguration configuration)
        {
            _authorizeUrl = configuration["BackEndURLs.Authorize"];
            _loginUrl = configuration["BackEndURLs.Login"];
            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // This method is used to login a user, by executing a call to Backend login endpoint.
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

        // This method is used to check authorization header validity, by executing a call to Backend autorization endpoint.
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
    }
}
