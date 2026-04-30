using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Models;
using TravelcardFunctionApp.Helpers;

namespace TravelcardFunctionApp.Functions
{
    public class TravelcardFunction
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger _logger;

        public TravelcardFunction(DbHelper dbHelper, ILoggerFactory loggerFactory)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _logger = loggerFactory?.CreateLogger<TravelcardFunction>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        [Function("CreateTravelcard")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "travelcard")] HttpRequestData req, FunctionContext executionContext)
        {
            _logger.LogInformation("Enter TravelcardFunction.Run");
            try
            {
                // Header checks
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    var resp401 = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "MissingHeader", Message = "Header 'client_id' is required." };
                    await resp401.WriteAsJsonAsync(err);
                    _logger.LogWarning("Missing client_id header");
                    return resp401;
                }

                string clientId = clientIds.FirstOrDefault() ?? string.Empty;
                if (clientId.Length < 1 || clientId.Length > 128)
                {
                    var resp400 = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "InvalidHeader", Message = "Header 'client_id' must be between 1 and 128 characters." };
                    await resp400.WriteAsJsonAsync(err);
                    _logger.LogWarning("Invalid client_id header");
                    return resp400;
                }

                req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correlationIds);
                string correlationId = correlationIds?.FirstOrDefault() ?? string.Empty;

                // Body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "InvalidRequest", Message = "Request body is required and must be application/json." };
                    await resp.WriteAsJsonAsync(err);
                    _logger.LogWarning("Empty request body");
                    return resp;
                }

                TravelcardRequest travelcard;
                try
                {
                    travelcard = JsonSerializer.Deserialize<TravelcardRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    var err = new ErrorResponse { Code = "BadJson", Message = "Invalid JSON payload.", Details = ex.Message };
                    await resp.WriteAsJsonAsync(err);
                    _logger.LogError(ex, "Invalid JSON");
                    return resp;
                }

                var validation = ValidationHelper.ValidateTravelcardRequest(travelcard);
                if (validation.Errors.Count > 0)
                {
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await resp.WriteAsJsonAsync(new { errors = validation.Errors });
                    _logger.LogWarning("Validation failed: {Count} errors", validation.Errors.Count);
                    return resp;
                }

                // Insert into DB
                try
                {
                    var token = TokenHelper.GenerateToken(6);
                    int travelcardId = await _dbHelper.InsertTravelcardAsync(travelcard);

                    var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                    await response.WriteAsJsonAsync(new { travelcardId = Guid.NewGuid().ToString(), token = token });
                    _logger.LogInformation("Travelcard created successfully. TravelcardId: {Id}", travelcardId);
                    _logger.LogInformation("Exit TravelcardFunction.Run");
                    return response;
                }
                catch (DbException dex)
                {
                    _logger.LogError(dex, "Database error while creating travelcard");
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    var err = new ErrorResponse { Code = "DatabaseError", Message = "An error occurred while saving the travelcard.", Details = dex.Message };
                    await resp.WriteAsJsonAsync(err);
                    return resp;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error");
                    var resp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    var err = new ErrorResponse { Code = "ServerError", Message = "An unexpected error occurred.", Details = ex.Message };
                    await resp.WriteAsJsonAsync(err);
                    return resp;
                }
            }
            finally
            {
                _logger.LogInformation("Exit TravelcardFunction.Run completed");
            }
        }
    }
}
