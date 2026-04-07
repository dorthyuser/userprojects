using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using TcTesting9Lambda.Services;
using TcTesting9Lambda.Models;

[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TcTesting9Lambda
{
    public class Function
    {
        private readonly Service _service;

        public Function()
        {
            _service = new Service();
        }

        /// <summary>
        /// Lambda handler method must be named "TcTesting9" as required.
        /// The aws-lambda-tools-defaults.json function-handler remains as provided.
        /// </summary>
        public async Task<APIGatewayProxyResponse> TcTesting9(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("Handler invoked: TcTesting9");
            try
            {
                // Extract headers, query params and path params for logging and validation
                Console.WriteLine($"Headers: {JsonSerializer.Serialize(request?.Headers)}");
                Console.WriteLine($"QueryStringParameters: {JsonSerializer.Serialize(request?.QueryStringParameters)}");
                Console.WriteLine($"PathParameters: {JsonSerializer.Serialize(request?.PathParameters)}");

                var serviceResponse = await _service.HandleAsync(request, context);

                var body = JsonSerializer.Serialize(serviceResponse, new JsonSerializerOptions { WriteIndented = false });

                var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };

                return new APIGatewayProxyResponse
                {
                    StatusCode = serviceResponse.StatusCode ?? 200,
                    Body = body,
                    Headers = headers
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception in Lambda handler: {ex}");
                var error = new Response
                {
                    Success = false,
                    Message = "Unhandled error in function",
                    Error = ex.Message,
                    StatusCode = 500
                };

                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = JsonSerializer.Serialize(error),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
        }
    }
}
