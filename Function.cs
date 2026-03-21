using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Ddctravelcard2026Lambda.Models;
using Ddctravelcard2026Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Ddctravelcard2026Lambda
{
    public class Function
    {
        private readonly Service _service;

        public Function()
        {
            _service = new Service();
        }

        /// <summary>
        /// Lambda handler must be named Ddctravelcard2026
        /// </summary>
        public async Task<APIGatewayProxyResponse> ddctravelcard2026(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("Received request");

            try
            {
                // Extract headers if present
                request.Headers ??= new System.Collections.Generic.Dictionary<string, string>();
                request.QueryStringParameters ??= new System.Collections.Generic.Dictionary<string, string>();

                // Validate header presence and patterns as per spec (only check existence/conditions)
                if (!request.Headers.ContainsKey("client_id") || string.IsNullOrWhiteSpace(request.Headers["client_id"]) || request.Headers["client_id"].Length > 128)
                {
                    return _service.CreateErrorResponse(400, "Invalid or missing header: client_id");
                }

                if (request.Headers.ContainsKey("X-Correlation-Cust-Id") && request.Headers["X-Correlation-Cust-Id"].Length > 100)
                {
                    return _service.CreateErrorResponse(400, "Invalid header: X-Correlation-Cust-Id too long");
                }

                if (request.Headers.ContainsKey("Content-Type") && request.Headers["Content-Type"] != "application/json")
                {
                    Console.WriteLine("Non-json content-type provided");
                }

                if (string.IsNullOrWhiteSpace(request.Body))
                    return _service.CreateErrorResponse(400, "Request body is required");

                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                var input = JsonSerializer.Deserialize<Request>(request.Body, options);
                if (input == null)
                    return _service.CreateErrorResponse(400, "Invalid request payload");

                // Run validations
                var validation = _service.ValidateRequest(input);
                if (!validation.IsValid)
                {
                    return _service.CreateErrorResponse(400, validation.ErrorMessage);
                }

                // Persist to DB
                var result = await _service.CreateTravelcardAsync(input);

                var response = new Response
                {
                    TravelcardId = result.TravelcardId,
                    Token = result.Token
                };

                var body = JsonSerializer.Serialize(response, options);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 201,
                    Body = body,
                    Headers = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error: {ex}");
                return _service.CreateErrorResponse(500, "Internal server error");
            }
        }
    }
}
