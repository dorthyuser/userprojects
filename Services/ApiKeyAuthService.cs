using System;

namespace AgeApi.Services
{
    public class ApiKeyAuthService : IAuthService
    {
        private readonly string _configuredKey;

        public ApiKeyAuthService()
        {
            _configuredKey = Environment.GetEnvironmentVariable("API_KEY") ?? string.Empty;
        }

        public bool ValidateApiKey(string? apiKey)
        {
            if (string.IsNullOrEmpty(_configuredKey)) return false;
            if (string.IsNullOrEmpty(apiKey)) return false;
            return string.Equals(_configuredKey, apiKey, StringComparison.Ordinal);
        }
    }
}
