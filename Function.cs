using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using UpTransportTicketApiLambda.Services;
using UpTransportTicketApiLambda.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace UpTransportTicketApiLambda
{
    public class Function
    {
        private readonly Service _service;

        public Function()
        {
            try
            {
                Console.WriteLine("Initializing function and reading configuration...");
                var json = File.ReadAllText("appsettings.json");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var conn = root.GetProperty("ConnectionStrings").GetProperty("PostgreSql").GetString() ?? string.Empty;
                _service = new Service(conn);
                Console.WriteLine("Service initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize service: {ex}");
                throw;
            }
        }

        public async Task<APIGatewayProxyResponse> UpTransportTicketApi(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                Console.WriteLine($"Received request: Method={request.HttpMethod} Path={request.Path}");

                // Basic routing by path
                var path = request.Path ?? string.Empty;
                var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && segments.Length == 1 && segments[0] == "tickets")
                {
                    // Create
                    var req = JsonSerializer.Deserialize<Request>(request.Body ?? string.Empty, Service.JsonOptions) ?? throw new Exception("Invalid request body");
                    var created = await _service.CreateTicketAsync(req);
                    return SuccessResponse(created);
                }

                if (segments.Length >= 2 && segments[0] == "tickets")
                {
                    if (Guid.TryParse(segments[1], out var id))
                    {
                        if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) && segments.Length == 2)
                        {
                            var res = await _service.GetTicketAsync(id);
                            if (res == null) return NotFoundResponse($"Ticket {id} not found");
                            return SuccessResponse(res);
                        }

                        if (string.Equals(request.HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase) && segments.Length == 2)
                        {
                            var req = JsonSerializer.Deserialize<Request>(request.Body ?? string.Empty, Service.JsonOptions) ?? throw new Exception("Invalid request body");
                            var updated = await _service.UpdateTicketAsync(id, req);
                            if (updated == null) return NotFoundResponse($"Ticket {id} not found");
                            return SuccessResponse(updated);
                        }

                        if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && segments.Length == 3 && segments[2] == "expire")
                        {
                            var expired = await _service.ExpireTicketAsync(id);
                            if (expired == null) return NotFoundResponse($"Ticket {id} not found");
                            return SuccessResponse(expired);
                        }

                        if (string.Equals(request.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase) && segments.Length == 2)
                        {
                            var deleted = await _service.DeleteTicketAsync(id);
                            if (!deleted) return NotFoundResponse($"Ticket {id} not found");
                            return new APIGatewayProxyResponse
                            {
                                StatusCode = (int)HttpStatusCode.NoContent,
                                Body = string.Empty,
                                Headers = new Dictionary<string, string> {{ "Content-Type", "application/json" }}
                            };
                        }
                    }
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = JsonSerializer.Serialize(new { error = new { message = "Unsupported route or invalid id" } }, Service.JsonOptions),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error: {ex}");
                var err = new { error = new { message = ex.Message, detail = ex.ToString() } };
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = JsonSerializer.Serialize(err, Service.JsonOptions),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
        }

        private APIGatewayProxyResponse SuccessResponse(object body)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize(body, Service.JsonOptions),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        private APIGatewayProxyResponse NotFoundResponse(string message)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Body = JsonSerializer.Serialize(new { error = new { message } }, Service.JsonOptions),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
