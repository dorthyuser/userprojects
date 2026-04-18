using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ZohoCrmOauthFinal1.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UserService> _logger;

        public UserService(IHttpClientFactory httpFactory, ITokenService tokenService, ILogger<UserService> logger)
        {
            _httpFactory = httpFactory;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<string> GetUsersAsync()
        {
            var client = _httpFactory.CreateClient("zoho-crm-connection");
            try
            {
                var token = await _tokenService.GetAccessTokenAsync();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var resp = await client.GetAsync("/crm/v2/users");
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed GET /crm/v2/users. Status: {Status}. Body: {Body}", resp.StatusCode, content);
                    throw new ApplicationException("Zoho CRM GET users failed");
                }
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Zoho CRM GET users");
                throw;
            }
        }

        public async Task<string> CreateUserAsync(object payload)
        {
            var client = _httpFactory.CreateClient("zoho-crm-connection");
            try
            {
                var token = await _tokenService.GetAccessTokenAsync();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                using var resp = await client.PostAsync("/crm/v2/users", new StringContent(json, Encoding.UTF8, "application/json"));
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed POST /crm/v2/users. Status: {Status}. Body: {Body}", resp.StatusCode, content);
                    throw new ApplicationException("Zoho CRM create user failed");
                }
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Zoho CRM create user");
                throw;
            }
        }

        public async Task<string> UpdateUserAsync(string id, object payload)
        {
            var client = _httpFactory.CreateClient("zoho-crm-connection");
            try
            {
                var token = await _tokenService.GetAccessTokenAsync();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                using var resp = await client.PutAsync($"/crm/v2/users/{id}", new StringContent(json, Encoding.UTF8, "application/json"));
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed PUT /crm/v2/users/{Id}. Status: {Status}. Body: {Body}", id, resp.StatusCode, content);
                    throw new ApplicationException("Zoho CRM update user failed");
                }
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Zoho CRM update user");
                throw;
            }
        }
    }
}