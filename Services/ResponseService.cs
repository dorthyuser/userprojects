using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResponseHttp.Services
{
    /// <summary>
    /// Implementation of IResponseService that uses IHttpClientFactory to obtain the configured HTTP client named "http".
    /// </summary>
    public class ResponseService : IResponseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ResponseService> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Construct the service with DI injected dependencies.
        /// </summary>
        public ResponseService(IHttpClientFactory httpClientFactory, ILogger<ResponseService> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Fetches the response from the configured HTTP client and returns the concatenated string.
        /// Throws if the remote call fails or if configuration is missing.
        /// </summary>
        public async Task<string> FetchAndFormatResponseAsync()
        {
            var client = _httpClientFactory.CreateClient("http");

            // Ensure a base address is configured or a specific endpoint is provided via configuration.
            // We expect either BaseAddress to be set by the Http connection registration or
            // a specific request path in configuration at "Http:RequestPath".
            string requestPath = _configuration["Http:RequestPath"]; // optional

            if (client.BaseAddress == null && string.IsNullOrEmpty(requestPath))
            {
                _logger.LogError("No base address configured for the 'http' client and no Http:RequestPath provided in configuration.");
                throw new System.InvalidOperationException("No endpoint configured for HTTP client.");
            }

            var requestUri = string.IsNullOrEmpty(requestPath) ? string.Empty : requestPath;

            try
            {
                var response = await client.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                // Concatenate result as requested.
                return $"Response received- <<{content}>>.";
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed when calling configured provider.");
                throw;
            }
        }
    }
}
