using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace azurecsharppost1112.Functions;

public class HealthFunction
{
    private readonly ILogger<HealthFunction> _logger;

    public HealthFunction(ILogger<HealthFunction> logger)
    {
        _logger = logger;
    }

    [Function("Health")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Entering Health function");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync("OK");
        _logger.LogInformation("Exiting Health function");
        return response;
    }
}
