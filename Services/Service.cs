using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;
using System.Text;
using System.Net;
using TcLambdaLambda.Models;

namespace TcLambdaLambda.Services
{
    public class Service
    {
        private readonly IAmazonSecretsManager _secretsClient;
        private readonly HttpClient _httpClient;

        public Service()
        {
            _secretsClient = new AmazonSecretsManagerClient();
            _httpClient = new HttpClient();
        }

        public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("[Service] Handling request");

            // Validate incoming request minimally
            if (request == null)
            {
                Console.WriteLine("[Service] Request is null");
                return CreateErrorResponse(HttpStatusCode.BadRequest, "invalid_request", "Request was null");
            }

            // Fetch secrets dynamically from AWS Secrets Manager
            SecretBundle secrets;
            try
            {
                secrets = await GetSecretsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Service] Token generation / secrets retrieval failed: {ex}");
                return CreateErrorResponse(HttpStatusCode.InternalServerError, "token_generation_failed", ex.Message);
            }

            if (string.IsNullOrEmpty(secrets.DownstreamUrl))
            {
                Console.WriteLine("[Service] Downstream URL not configured in secrets");
                return CreateErrorResponse(HttpStatusCode.InternalServerError, "configuration_error", "Downstream URL missing in secrets");
            }

            // Forward the request body exactly as received
            var payload = request.Body ?? string.Empty;

            try
            {
                var upstreamResponse = await CallExternalApiAsync(secrets, payload, request);

                // If upstream returned error (4xx, 5xx) we must preserve and return meaningful structured error
                if ((int)upstreamResponse.StatusCode >= 400)
                {
                    var upstreamBody = await upstreamResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Service] Upstream error: {(int)upstreamResponse.StatusCode} - {upstreamBody}");

                    var error = new
                    {
                        error = new
                        {
                            code = "upstream_error",
                            message = "Upstream Travelcard API returned an error",
                            upstreamStatus = (int)upstreamResponse.StatusCode,
                            upstreamBody = TryParseBody(upstreamBody)
                        }
                    };

                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)upstreamResponse.StatusCode,
                        Body = JsonSerializer.Serialize(error),
                        Headers = new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "Content-Type", "application/json" }
                        }
                    };
                }

                // Success: return upstream response body and status code
                var successBody = await upstreamResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[Service] Upstream success: {(int)upstreamResponse.StatusCode}");

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)upstreamResponse.StatusCode,
                    Body = successBody,
                    Headers = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Service] External API call failed: {ex}");
                return CreateErrorResponse(HttpStatusCode.BadGateway, "external_api_failure", ex.Message);
            }
        }

        private object TryParseBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            try
            {
                return JsonSerializer.Deserialize<object>(body);
            }
            catch
            {
                return body; // return raw string if not JSON
            }
        }

        private APIGatewayProxyResponse CreateErrorResponse(HttpStatusCode code, string errorCode, string message)
        {
            var resp = new
            {
                error = new
                {
                    code = errorCode,
                    message = message
                }
            };
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)code,
                Body = JsonSerializer.Serialize(resp),
                Headers = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }

        private async Task<HttpResponseMessage> CallExternalApiAsync(SecretBundle secrets, string payload, APIGatewayProxyRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, secrets.DownstreamUrl)
            {
                Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json")
            };

            // Mandatory headers for outgoing request
            httpRequest.Headers.Remove("client_id");
            httpRequest.Headers.Add("client_id", secrets.ClientId);
            httpRequest.Headers.Remove("Authorization");
            httpRequest.Headers.Add("Authorization", $"Bearer {secrets.AccessToken}");

            // If incoming request contains additional headers we might forward certain ones (e.g., tracing)
            if (request.Headers != null)
            {
                if (request.Headers.TryGetValue("X-Request-Id", out var rid))
                {
                    httpRequest.Headers.TryAddWithoutValidation("X-Request-Id", rid);
                }
            }

            Console.WriteLine($"[Service] Sending request to {secrets.DownstreamUrl}");
            var response = await _httpClient.SendAsync(httpRequest);
            return response;
        }

        private async Task<SecretBundle> GetSecretsAsync()
        {
            // The secret name must be provided via environment variable to avoid hardcoding credentials
            var secretName = Environment.GetEnvironmentVariable("SECRET_NAME");
            if (string.IsNullOrEmpty(secretName))
            {
                throw new InvalidOperationException("SECRET_NAME environment variable is not set");
            }

            Console.WriteLine($"[Service] Retrieving secret: {secretName}");
            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = secretName
            };

            var response = await _secretsClient.GetSecretValueAsync(request);
            var secretString = response.SecretString;
            if (string.IsNullOrEmpty(secretString))
            {
                throw new InvalidOperationException("Secret string is empty");
            }

            try
            {
                using var doc = JsonDocument.Parse(secretString);
                var root = doc.RootElement;

                var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() ?? string.Empty : string.Empty;
                var clientId = root.TryGetProperty("client_id", out var cid) ? cid.GetString() ?? string.Empty : string.Empty;
                var downstreamUrl = root.TryGetProperty("downstream_url", out var url) ? url.GetString() ?? string.Empty : string.Empty;

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(downstreamUrl))
                {
                    throw new InvalidOperationException("Required secret values (access_token, client_id, downstream_url) are missing");
                }

                return new SecretBundle
                {
                    AccessToken = accessToken,
                    ClientId = clientId,
                    DownstreamUrl = downstreamUrl
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Service] Failed to parse secret JSON: {ex}");
                throw;
            }
        }

        // Secret container
        private class SecretBundle
        {
            public string AccessToken { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string DownstreamUrl { get; set; } = string.Empty;
        }
    }
}
