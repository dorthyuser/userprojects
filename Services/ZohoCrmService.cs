using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZohoProject3.Models;

namespace ZohoProject3.Services
{
    public class ZohoCrmService : IZohoCrmService
    {
        private readonly IZohoCrmConnection _connection;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public ZohoCrmService(IZohoCrmConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken)
        {
            using var response = await _connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API error: {(int)response.StatusCode} - {body}");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var list = new List<UserDto>();

                if (root.TryGetProperty("users", out var usersElement) || root.TryGetProperty("data", out usersElement))
                {
                    foreach (var item in usersElement.EnumerateArray())
                    {
                        var user = new UserDto
                        {
                            Id = item.GetPropertyOrNull("id"),
                            Name = item.GetPropertyOrNull("full_name") ?? item.GetPropertyOrNull("name"),
                            Email = item.GetPropertyOrNull("email"),
                            Role = item.GetPropertyOrNull("role"),
                            Country = item.GetPropertyOrNull("country")
                        };
                        list.Add(user);
                    }
                }

                return list;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Zoho users response", ex);
            }
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(new { users = new[] { request } });
            using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v2/users", payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API error: {(int)response.StatusCode} - {body}");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("users", out var usersElement) || root.TryGetProperty("data", out usersElement))
                {
                    var first = usersElement.EnumerateArray().FirstOrDefault();
                    if (first.ValueKind != JsonValueKind.Undefined)
                    {
                        return new UserDto
                        {
                            Id = first.GetPropertyOrNull("id"),
                            Name = first.GetPropertyOrNull("full_name") ?? first.GetPropertyOrNull("name"),
                            Email = first.GetPropertyOrNull("email"),
                            Role = first.GetPropertyOrNull("role"),
                            Country = first.GetPropertyOrNull("country")
                        };
                    }
                }

                throw new InvalidOperationException("Unexpected Zoho create response format");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Zoho create response", ex);
            }
        }

        public async Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(new { users = new[] { request } });
            var path = $"/crm/v2/users/{Uri.EscapeDataString(id)}";
            using var response = await _connection.SendAsync(HttpMethod.Put, path, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoho API error: {(int)response.StatusCode} - {body}");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("users", out var usersElement) || root.TryGetProperty("data", out usersElement))
                {
                    var first = usersElement.EnumerateArray().FirstOrDefault();
                    if (first.ValueKind != JsonValueKind.Undefined)
                    {
                        return new UserDto
                        {
                            Id = first.GetPropertyOrNull("id"),
                            Name = first.GetPropertyOrNull("full_name") ?? first.GetPropertyOrNull("name"),
                            Email = first.GetPropertyOrNull("email"),
                            Role = first.GetPropertyOrNull("role"),
                            Country = first.GetPropertyOrNull("country")
                        };
                    }
                }

                throw new InvalidOperationException("Unexpected Zoho update response format");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Zoho update response", ex);
            }
        }
    }

    internal static class JsonExtensions
    {
        public static string? GetPropertyOrNull(this JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String) return prop.GetString();
                return prop.ToString();
            }
            return null;
        }
    }
}
