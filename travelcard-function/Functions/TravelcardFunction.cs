using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardFunction.Helpers;
using TravelcardFunction.Models;

namespace TravelcardFunction.Functions
{
    public class TravelcardFunction
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger _logger;

        public TravelcardFunction(DatabaseHelper db, ILoggerFactory loggerFactory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = loggerFactory?.CreateLogger<TravelcardFunction>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        [Function("GetTravelcard")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "travelcard")] HttpRequestData req)
        {
            _logger.LogInformation("Enter: GetTravelcard");
            try
            {
                // Header validation
                if (!req.Headers.TryGetValues("client_id", out var clientIds))
                {
                    _logger.LogWarning("Missing client_id header");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing required header: client_id");
                }

                var clientId = System.Linq.Enumerable.FirstOrDefault(clientIds);
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    _logger.LogWarning("Invalid client_id header");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid client_id header");
                }

                // Optional correlation header
                req.Headers.TryGetValues("X-Correlation-Cust-Id", out var correlationValues);
                var correlationId = correlationValues != null ? System.Linq.Enumerable.FirstOrDefault(correlationValues) : null;
                if (!string.IsNullOrEmpty(correlationId) && correlationId.Length > 100)
                {
                    _logger.LogWarning("Invalid X-Correlation-Cust-Id header length");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid X-Correlation-Cust-Id header");
                }

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var travelcardNumber = query.Get("travelcardNumber");
                if (string.IsNullOrWhiteSpace(travelcardNumber))
                {
                    _logger.LogWarning("Missing travelcardNumber query parameter");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing required query parameter: travelcardNumber");
                }

                _logger.LogInformation("Fetching travelcard {TravelcardNumber} for client {ClientId} Correlation: {Correlation}", travelcardNumber, clientId, correlationId);

                var travelcard = await _db.GetTravelcardByNumberAsync(travelcardNumber);
                if (travelcard == null)
                {
                    _logger.LogInformation("Travelcard not found: {TravelcardNumber}", travelcardNumber);
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Travelcard not found");
                }

                var resp = req.CreateResponse(HttpStatusCode.OK);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(travelcard, DatabaseHelper.JsonOptions));

                _logger.LogInformation("Exit: GetTravelcard success for {TravelcardNumber}", travelcardNumber);
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTravelcard");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An unexpected error occurred");
            }
        }

        private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode status, string message)
        {
            var resp = req.CreateResponse(status);
            resp.Headers.Add("Content-Type", "application/json");
            var err = new { error = message };
            await resp.WriteStringAsync(JsonSerializer.Serialize(err));
            return resp;
        }
    }
}
