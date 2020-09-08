using BackEnd.Models;
using System.Collections.Generic;
using System.Linq;

namespace BackEnd.Authentication
{
    public static class ExtensionMethods
    {
        // This method returns a users list without their passwords.
        public static IEnumerable<User> WithoutPasswords(this IEnumerable<User> users)
        {
            return users.Select(x => x.WithoutPassword());
        }

        // This method is used to remove user's password.
        public static User WithoutPassword(this User user)
        {
            user.Password = null;
            return user;
        }
    }
}
