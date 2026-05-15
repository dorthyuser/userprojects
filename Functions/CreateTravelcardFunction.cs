using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TravelcardDb.Models;
using TravelcardDb.Services;

namespace TravelcardDb.Functions;

public class CreateTravelcardFunction
{
    private readonly ITravelcardDbService _service;
    private readonly ILogger<CreateTravelcardFunction> _logger;

    public CreateTravelcardFunction(ITravelcardDbService service, ILogger<CreateTravelcardFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("CreateTravelcardFunction")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "travelcard")] HttpRequestData req)
    {
        _logger.LogInformation("[CreateTravelcardFunction] Entering RunAsync");
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(JsonSerializer.Serialize(new { error = "Request body is required", details = "Empty body" }));
                _logger.LogInformation("[CreateTravelcardFunction] Exiting RunAsync with status {StatusCode}", (int)HttpStatusCode.BadRequest);
                return badRequest;
            }

            var result = await _service.ForwardTravelcardAsync(body);
            var response = req.CreateResponse((HttpStatusCode)result.StatusCode);
            if (!string.IsNullOrWhiteSpace(result.ContentType))
            {
                response.Headers.Add("Content-Type", result.ContentType);
            }
            await response.WriteStringAsync(result.Body ?? string.Empty);
            _logger.LogInformation("[CreateTravelcardFunction] Exiting RunAsync with status {StatusCode}", result.StatusCode);
            return response;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[CreateTravelcardFunction] RunAsync failed: {Message}", ex.Message);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unhandled error", details = ex.Message }));
            return response;
        }
    }
}