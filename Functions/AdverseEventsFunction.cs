using System;
using System.Net;
using System.Threading.Tasks;
using azurefunctionaeproject.Helpers;
using azurefunctionaeproject.Models;
using azurefunctionaeproject.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace azurefunctionaeproject.Functions;

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
            var request = await JsonHelper.ReadFromJsonAsync<AdverseEventCreateRequest>(req);
            var result = await _service.SubmitAsync(request);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await JsonHelper.WriteJsonAsync(response, result, HttpStatusCode.Created);
            _logger.LogInformation("Exiting SubmitAdverseEvent");
            return response;
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in SubmitAdverseEvent");
            return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.BadRequest, ex.Code, ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found in SubmitAdverseEvent");
            return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.NotFound, ex.Code, ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict in SubmitAdverseEvent");
            return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.Conflict, ex.Code, ex.Message, ex.ExistingAeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in SubmitAdverseEvent");
            return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.InternalServerError, "DB_ERROR", "An unexpected error occurred.");
        }
    }

    [Function("GetNotifications")]
    public async Task<HttpResponseData> GetNotifications([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/adverse-events/notifications")] HttpRequestData req)
    {
        _logger.LogInformation("Entering GetNotifications");
        try
        {
            var query = NotificationQueryParser.Parse(req.Url.Query);
            var result = await _service.GetNotificationsAsync(query);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await JsonHelper.WriteJsonAsync(response, result, HttpStatusCode.OK);
            _logger.LogInformation("Exiting GetNotifications");
            return response;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error in GetNotifications");
            return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.BadRequest, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetNotifications");
            return await ErrorResponseHelper.CreateAsync(req, HttpStatusCode.InternalServerError, "DB_ERROR", "An unexpected error occurred.");
        }
    }
}
