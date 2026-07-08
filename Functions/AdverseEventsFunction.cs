using System;
using System.Net;
using System.Threading.Tasks;
using AdverseEventReporter.Helpers;
using AdverseEventReporter.Models;
using AdverseEventReporter.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AdverseEventReporter.Functions;

public class AdverseEventsFunction
{
    private readonly ILogger<AdverseEventsFunction> _logger;
    private readonly AdverseEventService _service;

    public AdverseEventsFunction(ILogger<AdverseEventsFunction> logger, AdverseEventService service)
    {
        _logger = logger;
        _service = service;
    }

    [Function("SubmitAdverseEvent")]
    public async Task<HttpResponseData> SubmitAdverseEvent([HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/adverse-events")] HttpRequestData req)
    {
        _logger.LogInformation("Entering SubmitAdverseEvent");
        try
        {
            var payload = await JsonHelper.DeserializeAsync<AdverseEventCreateRequest>(req.Body);
            var result = await _service.SubmitAsync(payload);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            _logger.LogInformation("Exiting SubmitAdverseEvent");
            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "SubmitAdverseEvent failed with code {Code}", ex.Code);
            var response = req.CreateResponse(ex.StatusCode);
            await response.WriteAsJsonAsync(new ErrorResponse(ex.Code, ex.Message));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in SubmitAdverseEvent");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new ErrorResponse("DB_ERROR", "An unexpected error occurred."));
            return response;
        }
    }

    [Function("GetNotifications")]
    public async Task<HttpResponseData> GetNotifications([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/adverse-events/notifications")] HttpRequestData req)
    {
        _logger.LogInformation("Entering GetNotifications");
        try
        {
            var query = QueryHelper.ParseNotificationQuery(req.Url.Query);
            var result = await _service.GetNotificationsAsync(query);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            _logger.LogInformation("Exiting GetNotifications");
            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "GetNotifications failed with code {Code}", ex.Code);
            var response = req.CreateResponse(ex.StatusCode);
            await response.WriteAsJsonAsync(new ErrorResponse(ex.Code, ex.Message));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetNotifications");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new ErrorResponse("DB_ERROR", "An unexpected error occurred."));
            return response;
        }
    }
}
