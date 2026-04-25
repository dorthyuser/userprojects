using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZohoProject2.Models;

namespace ZohoProject2.Services
{
    public class ZohoCrmService : IZohoCrmService
    {
        private readonly IZohoCrmConnection _connection;
        private readonly ILogger<ZohoCrmService> _logger;

        public ZohoCrmService(IZohoCrmConnection connection, ILogger<ZohoCrmService> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<object> GetUsersAsync(CancellationToken cancellationToken)
        {
            using var response = await _connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API error: {(int)response.StatusCode} - {body}");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                return JsonSerializer.Deserialize<object>(body) ?? new { };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Zoho response", ex);
            }
        }

        public async Task<object> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
        {
            var payload = new
            {
                users = new[]
                {
                    new
                    {
                        full_name = request.FullName,
                        email = request.Email
                    }
                }
            };

            string json;
            try
            {
                json = JsonSerializer.Serialize(payload);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to serialize request", ex);
            }

            using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v2/users", json, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API error: {(int)response.StatusCode} - {body}");
            }

            try
            {
                return JsonSerializer.Deserialize<object>(body) ?? new { };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Zoho response", ex);
            }
        }

        public async Task<object> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var payload = new
            {
                users = new[]
                {
                    new
                    {
                        id = id,
                        full_name = request.FullName,
                        email = request.Email
                    }
                }
            };

            string json;
            try
            {
                json = JsonSerializer.Serialize(payload);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to serialize request", ex);
            }

            var path = $"/crm/v2/users/{Uri.EscapeDataString(id)}";

            using var response = await _connection.SendAsync(HttpMethod.Put, path, json, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API error: {(int)response.StatusCode} - {body}");
            }

            try
            {
                return JsonSerializer.Deserialize<object>(body) ?? new { };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Zoho response", ex);
            }
        }
    }
}
