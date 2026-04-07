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
using System.Collections.Generic;

namespace TcLambdaLambda.Services
{
    public class Service
    {
        private readonly IAmazonSecretsManager _secretsClient;
        private static readonly HttpClient _httpClient = new HttpClient();

        public Service()
        {
            _secretsClient = new AmazonSecretsManagerClient();
        }

        public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("[Service] Handling request");

            if (request == null)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "invalid_request", "Request was null");
            }

            SecretBundle secrets;
            try
            {
                secrets = await GetSecretsAsync();
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(HttpStatusCode.InternalServerError, "secrets_failed", ex.Message);
            }

            var payload = request.Body ?? string.Empty;

            try
            {
                var upstreamResponse = await CallExternalApiAsync(secrets, payload, request);

                var responseBody = await upstreamResponse.Content.ReadAsStringAsync();

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)upstreamResponse.StatusCode,
                    Body = upstreamResponse.IsSuccessStatusCode
                        ? responseBody
                        : JsonSerializer.Serialize(new
                        {
                            error = new
                            {
                                code = "upstream_error",
                                status = (int)upstreamResponse.StatusCode,
                                body = TryParseBody(responseBody)
                            }
                        }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    }
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(HttpStatusCode.BadGateway, "external_api_failure", ex.Message);
            }
        }

        // 🔥 NEW: Token Generation
        private async Task<string> GetAccessTokenAsync(SecretBundle secrets)
        {
            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", secrets.ClientId),
                new KeyValuePair<string, string>("client_secret", secrets.ClientSecret),
                new KeyValuePair<string, string>("scope", secrets.Scope)
            });

            var response = await _httpClient.PostAsync(secrets.TokenUrl, body);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Service] Token failed: {content}");
                throw new Exception("Token generation failed");
            }

            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        private async Task<HttpResponseMessage> CallExternalApiAsync(SecretBundle secrets, string payload, APIGatewayProxyRequest request)
        {
            var token = await GetAccessTokenAsync(secrets); // ✅ FIX

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, secrets.DownstreamUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Add("client_id", secrets.ClientId);
            httpRequest.Headers.Add("Authorization", $"Bearer {token}");

            if (request.Headers != null && request.Headers.TryGetValue("X-Request-Id", out var rid))
            {
                httpRequest.Headers.TryAddWithoutValidation("X-Request-Id", rid);
            }

            return await _httpClient.SendAsync(httpRequest);
        }

        private async Task<SecretBundle> GetSecretsAsync()
        {
            var secretName = Environment.GetEnvironmentVariable("SECRET_NAME");
            if (string.IsNullOrEmpty(secretName))
                throw new Exception("SECRET_NAME not set");

            var response = await _secretsClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            });

            using var doc = JsonDocument.Parse(response.SecretString);
            var root = doc.RootElement;

            return new SecretBundle
            {
                ClientId = root.GetProperty("AZURE-CLIENT-ID").GetString(),
                ClientSecret = root.GetProperty("AZURE-CLIENT-SECRET").GetString(),
                TokenUrl = root.GetProperty("AZURE-TOKEN-URL").GetString(),
                Scope = root.GetProperty("AZURE-SCOPES").GetString(),
                DownstreamUrl = root.GetProperty("downstream_url").GetString()
            };
        }

        private object TryParseBody(string body)
        {
            try { return JsonSerializer.Deserialize<object>(body); }
            catch { return body; }
        }

        private APIGatewayProxyResponse CreateErrorResponse(HttpStatusCode code, string errorCode, string message)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)code,
                Body = JsonSerializer.Serialize(new
                {
                    error = new { code = errorCode, message }
                }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }

        private class SecretBundle
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string TokenUrl { get; set; }
            public string Scope { get; set; }
            public string DownstreamUrl { get; set; }
        }
    }
}
