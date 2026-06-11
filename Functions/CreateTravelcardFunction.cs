using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using create_travelcard_prod.Helpers;
using create_travelcard_prod.Models;

namespace create_travelcard_prod.Functions;

public class CreateTravelcardFunction
{
    private readonly ILogger<CreateTravelcardFunction> _logger;
    private readonly TravelcardRepository _repository;

    public CreateTravelcardFunction(ILogger<CreateTravelcardFunction> logger, TravelcardRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("CreateTravelcard")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("Entering CreateTravelcard function.");
        try
        {
            if (!req.Headers.TryGetValues("client_id", out var clientIds))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing required header: client_id");
            }

            var clientId = string.Join(",", clientIds);
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid header: client_id");
            }

            string requestBody;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = JsonSerializer.Deserialize<CreateTravelcardRequest>(requestBody, options);
            if (payload is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON payload.");
            }

            var result = await _repository.CreateAsync(payload, clientId);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            _logger.LogInformation("Exiting CreateTravelcard function successfully.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateTravelcard function.");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse { Error = new ErrorDetail { Code = ((int)statusCode).ToString(), Message = message } });
        return response;
    }
}
