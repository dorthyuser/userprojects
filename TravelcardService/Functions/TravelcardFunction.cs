using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardService.Helpers;

namespace TravelcardService.Functions
{
    public class TravelcardFunction
    {
        private readonly ITravelcardHttpHelper _travelcardHttpHelper;
        private readonly ILogger _logger;

        public TravelcardFunction(ITravelcardHttpHelper travelcardHttpHelper, ILoggerFactory loggerFactory)
        {
            _travelcardHttpHelper = travelcardHttpHelper ?? throw new ArgumentNullException(nameof(travelcardHttpHelper));
            _logger = loggerFactory?.CreateLogger<TravelcardFunction>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        [Function("TravelcardFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "travelcard")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("TravelcardFunction");
            logger.LogInformation("Entering TravelcardFunction");
            try
            {
                string body;
                using (var reader = new StreamReader(req.Body))
                {
                    body = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    logger.LogWarning("Empty request body received");
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    badResponse.Headers.Add("Content-Type", "application/json");
                    await badResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "InvalidRequest", message = "Request body must not be empty" })).ConfigureAwait(false);
                    logger.LogInformation("Exiting TravelcardFunction with BadRequest");
                    return badResponse;
                }
                logger.LogInformation("Incoming request URL: {Url}", req.Url);

                logger.LogInformation("Forwarding request to backend travelcard API");

                var backendResponse = await _travelcardHttpHelper.ForwardAsync(body);

                var response = req.CreateResponse(backendResponse.StatusCode);
                var content = await backendResponse.Content.ReadAsStringAsync();
                response.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                await response.WriteStringAsync(content);
                logger.LogInformation("backend response json: {backendResponse}", content);
                return response;
            }
            catch (BackendException bex)
            {
                logger.LogError(bex, "Backend returned an error");
                var resp = req.CreateResponse(System.Net.HttpStatusCode.BadGateway);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(new { error = "BackendError", message = bex.Message, details = bex.Details })).ConfigureAwait(false);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in TravelcardFunction");
                var resp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(new { error = "InternalServerError", message = ex.Message })).ConfigureAwait(false);
                return resp;
            }
        }
    }
}
