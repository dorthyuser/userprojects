using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using travelcard_service.Helpers;
using travelcard_service.Models;

namespace travelcard_service.Functions
{
    public class TravelcardFunction
    {
        private readonly OAuthTokenService _tokenService;
        private readonly HttpHelper _httpHelper;
        private readonly ILogger _logger;

        public TravelcardFunction(OAuthTokenService tokenService, HttpHelper httpHelper, ILoggerFactory loggerFactory)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _httpHelper = httpHelper ?? throw new ArgumentNullException(nameof(httpHelper));
            _logger = loggerFactory.CreateLogger<TravelcardFunction>();
        }

        [Function("TravelcardFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("TravelcardFunction");
            logger.LogInformation("Enter TravelcardFunction");

            try
            {
                using var reader = new StreamReader(req.Body);
                string requestBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Message = "Empty request body", StatusCode = 400 };
                    await badResponse.WriteAsJsonAsync(error);
                    logger.LogInformation("Exit TravelcardFunction with empty body");
                    return badResponse;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var travelcardRequest = JsonSerializer.Deserialize<TravelcardRequest>(requestBody, options);

                if (travelcardRequest == null || string.IsNullOrWhiteSpace(travelcardRequest.CardNumber) || string.IsNullOrWhiteSpace(travelcardRequest.HolderName))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    var error = new ErrorResponse { Message = "Invalid request payload. CardNumber and HolderName are required.", StatusCode = 400 };
                    await badResponse.WriteAsJsonAsync(error);
                    logger.LogInformation("Exit TravelcardFunction with validation error");
                    return badResponse;
                }

                logger.LogInformation("Validated request for CardNumber: {CardNumber}", travelcardRequest.CardNumber);

                logger.LogInformation("Acquiring access token from OAuth service");
                var tokenResult = await _tokenService.GetAccessTokenAsync();

                if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.AccessToken))
                {
                    var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                    var error = new ErrorResponse { Message = "Failed to obtain access token", StatusCode = 500, Details = tokenResult.Error };
                    await resp.WriteAsJsonAsync(error);
                    logger.LogError("Token acquisition failed: {Error}", tokenResult.Error);
                    return resp;
                }

                logger.LogInformation("Forwarding request to backend Travelcard API");
                var backendResponse = await _httpHelper.PostTravelcardAsync(travelcardRequest, tokenResult.AccessToken);

                if (!backendResponse.IsSuccess)
                {
                    var resp = req.CreateResponse((HttpStatusCode)backendResponse.StatusCode);
                    var error = new ErrorResponse { Message = "Backend API error", StatusCode = backendResponse.StatusCode, Details = backendResponse.Content };
                    await resp.WriteAsJsonAsync(error);
                    logger.LogError("Backend API returned error. Status: {Status}, Content: {Content}", backendResponse.StatusCode, backendResponse.Content);
                    return resp;
                }

                var success = req.CreateResponse(HttpStatusCode.OK);
                success.Headers.Add("Content-Type", "application/json");
                await success.WriteStringAsync(backendResponse.Content ?? "{}");

                logger.LogInformation("Exit TravelcardFunction successfully");
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in TravelcardFunction");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                var error = new ErrorResponse { Message = "Unhandled exception", StatusCode = 500, Details = ex.Message };
                await resp.WriteAsJsonAsync(error);
                return resp;
            }
        }
    }
}
