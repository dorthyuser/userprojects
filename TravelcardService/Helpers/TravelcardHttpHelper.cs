using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TravelcardService.Helpers
{
    public class TravelcardHttpHelper : ITravelcardHttpHelper
    {
        private readonly HttpClient _httpClient;
        private readonly IKeyVaultHelper _keyVaultHelper;
        private readonly ITokenService _tokenService;

        public TravelcardHttpHelper(
            HttpClient httpClient,
            IKeyVaultHelper keyVaultHelper,
            ITokenService tokenService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _keyVaultHelper = keyVaultHelper ?? throw new ArgumentNullException(nameof(keyVaultHelper));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<HttpResponseMessage> ForwardAsync(string body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(body))
                {
                    throw new ArgumentException("Request body cannot be empty");
                }

                // 🔐 Get base URL
                var baseUrl = await _keyVaultHelper.GetSecretAsync("TRAVELCARD_BASE_URL")
                    ?? throw new InvalidOperationException("TRAVELCARD_BASE_URL not found");

                // 🔐 Get client_id
                var clientId = await _keyVaultHelper.GetSecretAsync("AZURE-CLIENT-ID")
                    ?? throw new InvalidOperationException("AZURE-CLIENT-ID not found");

                // 🔐 Get OAuth token
                var token = await _tokenService.GetTokenAsync();

                // 🔑 Function key (ENV → fallback to KeyVault)
                var functionKey = Environment.GetEnvironmentVariable("TRAVELCARD_FUNCTION_KEY");

                if (string.IsNullOrWhiteSpace(functionKey))
                {
                    functionKey = await _keyVaultHelper.GetSecretAsync("TRAVELCARD_FUNCTION_KEY");
                }

                // 🔗 Build URL safely
                var baseUri = baseUrl.TrimEnd('/');
                var url = $"{baseUri}/api/travelcard";

                if (!string.IsNullOrWhiteSpace(functionKey))
                {
                    url += $"?code={functionKey}";
                }

                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    throw new InvalidOperationException($"Invalid URL constructed: {url}");
                }

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                // 📌 Required headers
                request.Headers.Add("client_id", clientId);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // 🚀 Send request
                var response = await _httpClient.SendAsync(request);

                // ✅ DO NOT throw on non-success
                return response;
            }
            catch (Exception ex)
            {
                throw new BackendException("Error forwarding request", ex.Message);
            }
        }
    }

    public class BackendException : Exception
    {
        public string Details { get; }

        public BackendException(string message, string details) : base(message)
        {
            Details = details;
        }
    }
}
