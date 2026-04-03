using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardGatewayService.Helpers;
using TravelcardGatewayService.Models;

namespace TravelcardGatewayService.Functions
{
    public class TravelcardFunction
    {
        private readonly ITravelcardHttpClient _travelcardHttpClient;

        public TravelcardFunction(ITravelcardHttpClient travelcardHttpClient)
        {
            _travelcardHttpClient = travelcardHttpClient;
        }

        [Function("TravelcardFunction")]
        public async Task<HttpResponseData> RunAsync([
            HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("TravelcardFunction");
            logger.LogInformation("Enter TravelcardFunction");

            try
            {
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    logger.LogWarning("Request body is empty");
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Error = "InvalidRequest", Details = "Request body is empty" };
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(error));
                    logger.LogInformation("Exit TravelcardFunction with BadRequest");
                    return badResp;
                }

                TravelcardRequest? requestModel;
                try
                {
                    requestModel = JsonSerializer.Deserialize<TravelcardRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deserialize request body");
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Error = "InvalidJson", Details = "Unable to parse JSON body" };
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(error));
                    logger.LogInformation("Exit TravelcardFunction with BadRequest (invalid JSON)");
                    return badResp;
                }

                if (requestModel == null)
                {
                    logger.LogWarning("Request model is null");
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Error = "InvalidRequest", Details = "Request body is invalid" };
                    await badResp.WriteStringAsync(JsonSerializer.Serialize(error));
                    return badResp;
                }    

                logger.LogInformation("Forwarding request to backend API");

                TravelcardResponse backendResponse = await _travelcardHttpClient.ForwardTravelcardAsync(requestModel);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteStringAsync(JsonSerializer.Serialize(backendResponse));
                logger.LogInformation("Exit TravelcardFunction with Success");
                return resp;
            }
            catch (BackendException bex)
            {
                var logger2 = context.GetLogger("TravelcardFunction");
                logger2.LogError(bex, "Backend error occurred");
                var resp = req.CreateResponse(HttpStatusCode.BadGateway);
                var error = new ErrorResponse { Error = "BackendError", Details = bex.Message };
                await resp.WriteStringAsync(JsonSerializer.Serialize(error));
                return resp;
            }
            catch (Exception ex)
            {
                var logger3 = context.GetLogger("TravelcardFunction");
                logger3.LogError(ex, "Unhandled exception in TravelcardFunction");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                var error = new ErrorResponse { Error = "ServerError", Details = "An unexpected error occurred" };
                await resp.WriteStringAsync(JsonSerializer.Serialize(error));
                return resp;
            }
        }
    }
}
