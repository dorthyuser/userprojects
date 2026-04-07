using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;

[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TcLambdaLambda
{
    public class Function
    {
        private readonly Services.Service _service;

        public Function()
        {
            _service = new Services.Service();
        }

        /// <summary>
        /// Lambda handler named TcLambda
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> TcLambda(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("[Function] Invoked TcLambda");
            try
            {
                return await _service.HandleAsync(request, context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Function] Unhandled error: {ex}");
                var error = new Models.Response
                {
                    Status = "error",
                    Message = "Unhandled exception",
                    Details = ex.Message
                };
                var body = System.Text.Json.JsonSerializer.Serialize(new { error });
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = body,
                    Headers = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    }
                };
            }
        }
    }
}
