using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace tc_testing_api2.Services
{
    /// <summary>
    /// Implements forwarding logic for Travelcard requests.
    /// </summary>
    public class TravelcardService : ITravelcardService
    {
        private readonly TcConnectionClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TravelcardService> _logger;

        public TravelcardService(TcConnectionClient client, IConfiguration configuration, ILogger<TravelcardService> logger)
        {
            _client = client;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> ForwardAsync(string body)
        {
            var apiUrl = _configuration["TRAVELCARD_API_URL"] ?? Environment.GetEnvironmentVariable("TRAVELCARD_API_URL");
            var functionKey = _configuration["TRAVELCARD_FUNCTION_KEY"] ?? Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY");

            if (string.IsNullOrEmpty(apiUrl))
            {
                throw new ArgumentException("TRAVELCARD_API_URL is not configured");
            }

            if (string.IsNullOrEmpty(functionKey))
            {
                throw new ArgumentException("TRAVELCARD_FUNCTION_KEY is not configured");
            }

            // Append function key as query parameter
            string url = apiUrl;
            if (!url.Contains("?"))
                url = $"{url.TrimEnd('/')}{(functionKey != null ? $"?code={Uri.EscapeDataString(functionKey)}" : string.Empty)}";
            else
                url = $"{url}&code={Uri.EscapeDataString(functionKey)}";

            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            var headers = new Dictionary<string, string>();
            var clientId = _configuration["AZURE-CLIENT-ID"] ?? Environment.GetEnvironmentVariable("AZURE-CLIENT-ID") ?? string.Empty;
            if (!string.IsNullOrEmpty(clientId))
            {
                headers["client_id"] = clientId;
            }

            _logger.LogInformation("Forwarding request to {Url}", url);
            var response = await _client.PostAsync(url, content, headers);
            return response;
        }
    }
}