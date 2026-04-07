using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using TcTesting9Lambda.Models;
using Amazon.Lambda.APIGatewayEvents;

namespace TcTesting9Lambda.Services
{
    public class SecretsData
    {
        public string? ClientId { get; set; }
        public string? Token { get; set; }
        public string? DownstreamUrl { get; set; }
    }

    public class Service
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretName;
        private readonly string _region;

        public Service()
        {
            _httpClient = new HttpClient();

            // Load defaults from environment or appsettings via environment variables
            _secretName = Environment.GetEnvironmentVariable("SecretName") ?? "tc-testing9/secrets";
            _region = Environment.GetEnvironmentVariable("Region") ?? "us-east-1";

            Console.WriteLine($"Service initialized. SecretName={_secretName}, Region={_region}");
        }

        public async Task<Response> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("Service.HandleAsync: started");
            try
            {
                // Validate request basic shape
                var validation = ValidateRequest(request);
                if (!validation.Success)
                {
                    Console.WriteLine("Validation failed: " + validation.Error);
                    return validation;
                }

                var secrets = await GetSecretsAsync();
                if (secrets == null)
                {
                    Console.WriteLine("Secrets retrieval failed");
                    return new Response { Success = false, Message = "Failed to retrieve secrets", Error = "Secrets not found", StatusCode = 500 };
                }

                // Prepare outbound request
                var downstreamUrl = secrets.DownstreamUrl ?? Environment.GetEnvironmentVariable("DownstreamDefaultUrl") ?? "";
                if (string.IsNullOrEmpty(downstreamUrl))
                {
                    Console.WriteLine("Downstream URL missing");
                    return new Response { Success = false, Message = "Downstream URL missing", Error = "Configuration error", StatusCode = 500 };
                }

                var outgoing = new HttpRequestMessage(HttpMethod.Post, downstreamUrl);

                // Ensure required headers for outgoing request
                var clientId = secrets.ClientId ?? GetHeaderValue(request?.Headers, "client_id");
                if (!string.IsNullOrEmpty(clientId))
                {
                    // Use required header name as-is
                    if (!outgoing.Headers.Contains("client_id"))
                        outgoing.Headers.Add("client_id", clientId);
                }

                // Content-Type and body
                var bodyContent = request?.Body ?? string.Empty;
                outgoing.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");

                // Authorization header
                var tokenValue = secrets.Token ?? GetHeaderValue(request?.Headers, "Authorization");
                if (!string.IsNullOrEmpty(tokenValue))
                {
                    // If tokenValue already contains "Bearer ", ensure we don't duplicate
                    var trimmed = tokenValue.Trim();
                    if (!trimmed.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        trimmed = "Bearer " + trimmed;
                    }
                    // Set Authorization header on the request
                    outgoing.Headers.Authorization = AuthenticationHeaderValue.Parse(trimmed);
                }

                Console.WriteLine($"Forwarding request to downstream: {downstreamUrl}");
                Console.WriteLine($"Outgoing headers: client_id={(clientId ?? "(none)")}, Authorization={(tokenValue != null ? "(present)" : "(none)" )}");

                var responseMessage = await _httpClient.SendAsync(outgoing);
                var respBody = await responseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"Downstream returned status {(int)responseMessage.StatusCode}");

                if (responseMessage.IsSuccessStatusCode)
                {
                    JsonElement data;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(respBody))
                        {
                            data = JsonDocument.Parse("null").RootElement;
                        }
                        else
                        {
                            data = JsonSerializer.Deserialize<JsonElement>(respBody);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse downstream JSON: {ex}");
                        data = JsonDocument.Parse(JsonSerializer.Serialize(new { raw = respBody })).RootElement;
                    }

                    return new Response
                    {
                        Success = true,
                        Message = "OK",
                        Data = data,
                        StatusCode = (int)responseMessage.StatusCode
                    };
                }
                else
                {
                    Console.WriteLine($"Downstream error: {(int)responseMessage.StatusCode} - {respBody}");
                    return new Response
                    {
                        Success = false,
                        Message = "Downstream error",
                        Error = respBody,
                        StatusCode = (int)responseMessage.StatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Service.HandleAsync exception: {ex}");
                return new Response { Success = false, Message = "Internal server error", Error = ex.Message, StatusCode = 500 };
            }
        }

        private Response ValidateRequest(APIGatewayProxyRequest request)
        {
            Console.WriteLine("Validating request");
            try
            {
                // If a Content-Type header is provided in the incoming request validate it's application/json
                if (request.Headers != null)
                {
                    // Case-insensitive header lookup
                    var ct = GetHeaderValue(request.Headers, "Content-Type");
                    if (!string.IsNullOrEmpty(ct))
                    {
                        var contentType = ct.ToLowerInvariant();
                        if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("application/json"))
                        {
                            return new Response { Success = false, Message = "Invalid Content-Type", Error = "Only application/json is supported", StatusCode = 400 };
                        }
                    }
                }

                // No strict schema is enforced here. The full body is forwarded as-is.
                return new Response { Success = true };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation exception: {ex}");
                return new Response { Success = false, Message = "Validation error", Error = ex.Message, StatusCode = 400 };
            }
        }

        private async Task<SecretsData?> GetSecretsAsync()
        {
            Console.WriteLine($"Retrieving secrets from Secrets Manager: {_secretName}");
            try
            {
                using var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(_region));
                var request = new GetSecretValueRequest { SecretId = _secretName };
                var response = await client.GetSecretValueAsync(request);

                string secretString = response.SecretString ?? string.Empty;
                if (string.IsNullOrEmpty(secretString))
                {
                    Console.WriteLine("Secret string empty");
                    return null;
                }

                try
                {
                    var doc = JsonSerializer.Deserialize<JsonElement>(secretString);
                    var data = new SecretsData();

                    if (doc.TryGetProperty("client_id", out var clientId)) data.ClientId = clientId.GetString();
                    if (doc.TryGetProperty("token", out var token)) data.Token = token.GetString();
                    if (doc.TryGetProperty("downstream_url", out var url)) data.DownstreamUrl = url.GetString();

                    Console.WriteLine("Secrets parsed successfully");
                    return data;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse secret JSON: {ex}");
                    return null;
                }
            }
            catch (ResourceNotFoundException ex)
            {
                Console.WriteLine($"Secret not found: {ex}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving secret: {ex}");
                return null;
            }
        }

        // Helper to safely get header values in a case-insensitive manner without relying on extension methods
        private static string? GetHeaderValue(IDictionary<string, string>? headers, string key)
        {
            if (headers == null) return null;
            foreach (var kvp in headers)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            return null;
        }
    }
}
