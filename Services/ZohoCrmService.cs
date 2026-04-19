using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZohoProject1.Models;

namespace ZohoProject1.Services
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

        public async Task<string> GetUsersAsync(CancellationToken cancellationToken)
        {
            using var response = await _connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API error: {response.StatusCode} - {content}");
            }

            return content;
        }

        public async Task<string> CreateUserAsync(User user, CancellationToken cancellationToken)
        {
            var payload = new { users = new[] { user } };
            var json = JsonSerializer.Serialize(payload);

            using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v2/users", json, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API create error: {response.StatusCode} - {content}");
            }

            return content;
        }

        public async Task<string> UpdateUserAsync(string id, User user, CancellationToken cancellationToken)
        {
            var payload = new { users = new[] { user } };
            var json = JsonSerializer.Serialize(payload);
            var path = $"/crm/v2/users/{Uri.EscapeDataString(id)}";

            using var response = await _connection.SendAsync(HttpMethod.Put, path, json, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API update error: {response.StatusCode} - {content}");
            }

            return content;
        }
    }
}
