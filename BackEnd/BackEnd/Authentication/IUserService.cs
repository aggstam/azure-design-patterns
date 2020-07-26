using BackEnd.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.Authentication
{
    public interface IUserService
    {
        Task<User> Authenticate(string username, string password);
        Task<IEnumerable<User>> GetAll();
        Task<bool> CreateUser(User user);
    }

    public class UserService : IUserService
    {

        private readonly IMongoCollection<User> _users;
        public UserService(IUsersDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>(settings.UsersCollectionName);
        }

        public async Task<User> Authenticate(string username, string password)
        {
            var user = await Task.Run(() => _users.Find<User>(user => user.Username == username && user.Password == password).FirstOrDefault());
            if (user == null) { return null; }
            return user.WithoutPassword();
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return await Task.Run(() => _users.Find(user => true).ToList().WithoutPasswords());
        }

        public async Task<bool> CreateUser(User user)
        {
            var existingUser = await Task.Run(() => _users.Find<User>(existingUser => existingUser.Username == user.Username).FirstOrDefault());
            if (existingUser != null) { return false; }
            _users.InsertOne(user);
            return true;
        }
    }
}
