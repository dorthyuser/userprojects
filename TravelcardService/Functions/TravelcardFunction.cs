using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardService.Helpers;
using TravelcardService.Models;

namespace TravelcardService.Functions
{
    public class TravelcardFunction
    {
        private readonly IHttpHelper _httpHelper;
        private readonly ILogger _logger;

        public TravelcardFunction(IHttpHelper httpHelper, ILogger<TravelcardFunction> logger)
        {
            _httpHelper = httpHelper ?? throw new ArgumentNullException(nameof(httpHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("TravelcardFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
        {
            _logger.LogInformation("Enter TravelcardFunction: received request to forward to Travelcard API");

            string rawBody;
            try
            {
                using var sr = new StreamReader(req.Body);
                rawBody = await sr.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading request body");
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteAsJsonAsync(new ErrorResponse { Error = new ErrorDetail { Message = "Invalid request body", Details = ex.Message } });
                _logger.LogInformation("Exit TravelcardFunction with BadRequest");
                return badResp;
            }

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                _logger.LogWarning("Empty request body");
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteAsJsonAsync(new ErrorResponse { Error = new ErrorDetail { Message = "Request body cannot be empty" } });
                _logger.LogInformation("Exit TravelcardFunction with BadRequest (empty body)");
                return badResp;
            }

            // Validate shape against DTO
            TravelcardRequest? dto = null;
            try
            {
                dto = JsonSerializer.Deserialize<TravelcardRequest>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payload does not match expected TravelcardRequest model");
            }

            if (dto == null)
            {
                // If DTO not present, still forward the raw body as required by spec but return a warning to caller
                _logger.LogInformation("Payload not matching DTO; will forward raw payload as received");
            }

            try
            {
                var backendResponse = await _httpHelper.PostToTravelcardAsync(rawBody);

                var successResp = req.CreateResponse((HttpStatusCode)backendResponse.StatusCode);
                foreach (var header in backendResponse.Headers)
                {
                    // skip restricted headers; this is minimal pass-through
                    try
                    {
                        successResp.Headers.Add(header.Key, string.Join(";", header.Value));
                    }
                    catch { }
                }

                var content = await backendResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Exit TravelcardFunction: forwarded successfully");

                if (string.IsNullOrEmpty(content))
                {
                    return successResp;
                }

                successResp.Headers.Add("Content-Type", "application/json");
                await successResp.WriteStringAsync(content);
                return successResp;
            }
            catch (BackendException bex)
            {
                _logger.LogError(bex, "Backend error while forwarding request");
                var resp = req.CreateResponse(HttpStatusCode.BadGateway);
                await resp.WriteAsJsonAsync(new ErrorResponse { Error = new ErrorDetail { Message = "Backend API error", Details = bex.Details, BackendStatus = bex.StatusCode } });
                _logger.LogInformation("Exit TravelcardFunction with BackendError");
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while forwarding request");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteAsJsonAsync(new ErrorResponse { Error = new ErrorDetail { Message = "Internal server error", Details = ex.Message } });
                _logger.LogInformation("Exit TravelcardFunction with InternalServerError");
                return resp;
            }
        }
    }
}
