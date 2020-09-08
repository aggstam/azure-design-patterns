using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace GateKeeper.Validation
{
    public interface IValidationService
    {
        List<string> ValidateCredentials(string username, string password);
        bool ValidateCaller(string authHeader, string username);
        string ValidateFile(IFormFile file);
        string ValidateValetKeyLifeTime(string lifeTimeString);
    }

    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private readonly Regex _usernamePattern; // Valid username pattern.
        private readonly int _usernameMinLength; // Valid username min length.
        private readonly Regex _passwordPattern; // Valid password pattern.
        private readonly int _passwordMinLength; // Valid password min length.
        private readonly List<string> _fileTypes; // Accepted file types.
        private readonly long _fileMaxSize; // Accepted file max size.
        private readonly double _valetKeyMaxLifetime; // Valid Valet key max duration.

        public ValidationService(IConfiguration configuration, ILogger<ValidationService> logger)
        {
            _logger = logger;
            _usernamePattern = new Regex(configuration["Validation.Pattern.Username"]);
            _usernameMinLength = int.Parse(configuration["Validation.MinLength.Username"]);
            _passwordPattern = new Regex(configuration["Validation.Pattern.Password"]);
            _passwordMinLength = int.Parse(configuration["Validation.MinLength.Password"]);
            _fileTypes = new List<string>(configuration["Validation.File.Types"].Split(new char[] { ';' }));
            _fileMaxSize = long.Parse(configuration["Validation.File.MaxSize"]);
            _valetKeyMaxLifetime = double.Parse(configuration["Validation.ValetKey.MaxLifeTime"]);
        }

        // This method is used to validate user credentials, based on predifined rules.
        public List<string> ValidateCredentials(string username, string password)
        {
            List<string> validationErrors = new List<string>(); 
            if (username == null || username.Trim().Length == 0 || password == null || password.Trim().Length == 0)
            {
                validationErrors.Add("Username or password cannot be empty.");
                return validationErrors;
            }
            if (!_usernamePattern.IsMatch(username)) { validationErrors.Add(string.Format("Username must follow the pattern: {0}", _usernamePattern)); }
            if (username.Length < _usernameMinLength) { validationErrors.Add(string.Format("Username length must be greater or equal to {0} characters.", _usernameMinLength)); }
            if (!_passwordPattern.IsMatch(password)) { validationErrors.Add(string.Format("Password must follow the pattern: {0}", _passwordPattern)); }
            if (password.Length < _passwordMinLength) { validationErrors.Add(string.Format("Password length must be greater or equal to {0} characters.", _usernameMinLength)); }
            return validationErrors;
        }

        // This method is used to validate that caller(username) matches the authorization header user.
        public bool ValidateCaller(string authHeaderString, string username)
        {
            var authHeader = AuthenticationHeaderValue.Parse(authHeaderString);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            return username == credentials[0];
        }

        // This method is used to validate a file, based on predifined rules.
        public string ValidateFile(IFormFile file)
        {
            if (file == null) { return "Form did not contained a file."; }
            if (!file.ContentType.StartsWith("image")) { return "File is not an image."; }
            string extension = Path.GetExtension(file.FileName);
            if (extension == string.Empty) { return "File does not have an extension."; }
            if (!_fileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase)) { return string.Format("File type is not accepted. Accepted types: {0}", String.Join(", ", _fileTypes)); }
            if (file.Length > _fileMaxSize) { return string.Format("File size exceeds limit. Max file size: {0}", ((_fileMaxSize / 1024f) / 1024f).ToString("0.00")); }
            return null;
        }

        // This method is used to validate Valet key duration, based on predifined rules.
        public string ValidateValetKeyLifeTime(string lifeTimeString)
        {
            if (lifeTimeString != null && lifeTimeString.Trim().Length != 0)
            {
                try
                {
                    double lifeTime = double.Parse(lifeTimeString.Trim());
                    if (lifeTime <= 0) { return "Valet Key life time must be a positive number."; }
                    if (lifeTime > _valetKeyMaxLifetime) { return string.Format("Valet Key life time exceeds limit. Max life time: {0}", _valetKeyMaxLifetime); }
                }
                catch (Exception)
                {
                    return "Valet Key life time must be a positive number.";
                }
            }
            return null;
        }
    }
}
