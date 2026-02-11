using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesforceAccountFunctions.Models;

namespace SalesforceAccountFunctions.Helpers.Salesforce
{
    public class SalesforceClient : ISalesforceClient
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalesforceClient> _logger;
        private string? _accessToken;
        private DateTimeOffset _accessTokenExpiry = DateTimeOffset.MinValue;
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public SalesforceClient(IHttpClientFactory httpFactory, IConfiguration configuration, ILogger<SalesforceClient> logger)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string GetInstanceUrl()
        {
            var inst = _configuration["SF_INSTANCE_URL"];
            if (string.IsNullOrEmpty(inst)) inst = "https://login.salesforce.com";
            return inst.TrimEnd('/');
        }

        private async Task EnsureAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _accessTokenExpiry.AddSeconds(-30))
            {
                return;
            }

            var clientId = _configuration["SF_CLIENT_ID"];
            var clientSecret = _configuration["SF_CLIENT_SECRET"];
            var username = _configuration["SF_USERNAME"];
            var password = _configuration["SF_PASSWORD"]; // password + security token appended if needed

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Salesforce credentials are not configured properly in environment variables.");
            }

            var tokenEndpoint = GetInstanceUrl() + "/services/oauth2/token";
            var client = _httpFactory.CreateClient();
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var resp = await client.PostAsync(tokenEndpoint, form).ConfigureAwait(false);
            var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to obtain Salesforce access token: {status} {content}", resp.StatusCode, content);
                throw new InvalidOperationException($"Unable to authenticate to Salesforce: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.TryGetProperty("access_token", out var tokenEl))
            {
                _accessToken = tokenEl.GetString();
                var instanceUrl = root.TryGetProperty("instance_url", out var instanceEl) ? instanceEl.GetString() : GetInstanceUrl();
                _configuration["SF_INSTANCE_URL"] = instanceUrl ?? GetInstanceUrl();

                // Token expiry handling - Salesforce doesn't always return expires_in for password flow. We'll set a conservative expiry.
                if (root.TryGetProperty("expires_in", out var expiresEl) && expiresEl.TryGetInt32(out var expiresInt))
                {
                    _accessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresInt);
                }
                else
                {
                    _accessTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(10); // conservative default
                }
            }
            else
            {
                _logger.LogError("Salesforce token response missing access_token: {content}", content);
                throw new InvalidOperationException("Salesforce token response did not contain an access_token");
            }
        }

        public async Task<SalesforceCreateResult> CreateAccountAsync(AccountDto account)
        {
            try
            {
                await EnsureAccessTokenAsync().ConfigureAwait(false);
                var instance = GetInstanceUrl();
                var apiVersion = "v57.0";
                var url = $"{instance}/services/data/{apiVersion}/sobjects/Account";
                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var payload = new
                {
                    Name = account.Name,
                    Phone = account.Phone,
                    Website = account.Website,
                    BillingCity = account.BillingCity
                };

                var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(url, content).ConfigureAwait(false);
                var respContent = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(respContent);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("id", out var idEl))
                    {
                        return new SalesforceCreateResult { Success = true, Id = idEl.GetString() };
                    }
                    return new SalesforceCreateResult { Success = true, Id = null };
                }
                _logger.LogError("Salesforce create account failed: {status} {content}", resp.StatusCode, respContent);
                return new SalesforceCreateResult { Success = false, ErrorDetails = respContent };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAccountAsync exception");
                return new SalesforceCreateResult { Success = false, ErrorDetails = ex.Message };
            }
        }

        public async Task<AccountDto?> GetAccountAsync(string id)
        {
            try
            {
                await EnsureAccessTokenAsync().ConfigureAwait(false);
                var instance = GetInstanceUrl();
                var apiVersion = "v57.0";
                var url = $"{instance}/services/data/{apiVersion}/sobjects/Account/{id}";
                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var resp = await client.GetAsync(url).ConfigureAwait(false);
                var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    var el = JsonSerializer.Deserialize<AccountDto>(content, JsonOptions);
                    return el;
                }

                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                _logger.LogError("Salesforce get account failed: {status} {content}", resp.StatusCode, content);
                throw new InvalidOperationException(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAccountAsync exception");
                throw;
            }
        }

        public async Task<SalesforceDeleteResult> DeleteAccountAsync(string id)
        {
            try
            {
                await EnsureAccessTokenAsync().ConfigureAwait(false);
                var instance = GetInstanceUrl();
                var apiVersion = "v57.0";
                var url = $"{instance}/services/data/{apiVersion}/sobjects/Account/{id}";
                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var resp = await client.DeleteAsync(url).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    return new SalesforceDeleteResult { Success = true };
                }

                var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Salesforce delete account failed: {status} {content}", resp.StatusCode, content);
                return new SalesforceDeleteResult { Success = false, ErrorDetails = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAccountAsync exception");
                return new SalesforceDeleteResult { Success = false, ErrorDetails = ex.Message };
            }
        }
    }
}
