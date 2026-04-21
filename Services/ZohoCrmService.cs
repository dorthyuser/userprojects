using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using zoho_project_csharp.Models;

namespace zoho_project_csharp.Services
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

        public async Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken)
        {
            using var response = await _connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ((int)response.StatusCode, content);
        }

        public async Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
        {
            // Zoho Users API requires the "users" wrapper and string IDs for role/profile
            var payload = JsonSerializer.Serialize(new
            {
                users = new[]
                {
                    new {
                        first_name = request.FirstName,
                        last_name = request.LastName,
                        email = request.Email,
                        role = request.Role?.Id,    // Flattened to string
                        profile = request.Profile?.Id, // Flattened to string
                        country = "India",
                        locale = "en_IN",
                        time_zone = "Asia/Kolkata"
                    }
                }
            });

            using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v2/users", payload, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ((int)response.StatusCode, content);
        }

        public async Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            // Update also uses the "users" wrapper and string IDs
            var payload = JsonSerializer.Serialize(new
            {
                users = new[]
                {
                    new {
                        first_name = request.FirstName,
                        last_name = request.LastName,
                        email = request.Email,
                        role = request.Role?.Id,    // Flattened to string
                        profile = request.Profile?.Id // Flattened to string
                    }
                }
            });

            var path = $"/crm/v2/users/{Uri.EscapeDataString(id)}";
            using var response = await _connection.SendAsync(HttpMethod.Put, path, payload, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ((int)response.StatusCode, content);
        }
    }
}
