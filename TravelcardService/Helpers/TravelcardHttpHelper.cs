using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TravelcardService.Helpers
{
    public class TravelcardHttpHelper : ITravelcardHttpHelper
    {
        private readonly HttpClient _httpClient;
        private readonly IKeyVaultHelper _keyVaultHelper;
        private readonly ITokenService _tokenService;

        public TravelcardHttpHelper(HttpClient httpClient, IKeyVaultHelper keyVaultHelper, ITokenService tokenService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _keyVaultHelper = keyVaultHelper ?? throw new ArgumentNullException(nameof(keyVaultHelper));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<string> ForwardAsync(string body, ILogger logger)
        {
            logger.LogInformation("Enter TravelcardHttpHelper.ForwardAsync");
            try
            {
                string baseUrl = await _keyVaultHelper.GetSecretAsync("TRAVELCARD-BASE-URL").ConfigureAwait(false) ?? throw new InvalidOperationException("TRAVELCARD-BASE-URL not found in Key Vault");
                string clientId = await _keyVaultHelper.GetSecretAsync("AZURE-CLIENT-ID").ConfigureAwait(false) ?? throw new InvalidOperationException("AZURE-CLIENT-ID not found in Key Vault");

                string token = await _tokenService.GetTokenAsync().ConfigureAwait(false);

                var requestUri = new Uri(new Uri(baseUrl), "api/travelcard");
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("client_id", clientId);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                logger.LogInformation("Sending request to backend {Url}", requestUri);

                using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                string respBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Backend returned non-success status {StatusCode}. Response: {Response}", response.StatusCode, respBody);
                    throw new BackendException($"Backend returned {(int)response.StatusCode}", respBody);
                }

                logger.LogInformation("Exit TravelcardHttpHelper.ForwardAsync");
                return respBody;
            }
            catch (BackendException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error forwarding request to backend");
                throw new Exception("Error forwarding request to backend", ex);
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
