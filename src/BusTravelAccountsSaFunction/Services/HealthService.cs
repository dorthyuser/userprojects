using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

public sealed class HealthService(IHealthRepository repository, ILogger<HealthService> logger) : IHealthService
{
    public Task<IActionResult> AliveAsync(CancellationToken cancellationToken)
    {
        IActionResult result = new ContentResult
        {
            StatusCode = StatusCodes.Status200OK,
            Content = "UP",
            ContentType = "text/plain"
        };

        return Task.FromResult(result);
    }

    public async Task<IActionResult> ReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await repository.CheckDependenciesAsync(cancellationToken);
            logger.LogInformation("READINESS CHECK dependencies outcome: success");

            return new ContentResult
            {
                StatusCode = StatusCodes.Status200OK,
                Content = "UP",
                ContentType = "text/plain"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "READINESS CHECK dependencies outcome: failure");
            return new ContentResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Content = "DOWN",
                ContentType = "text/plain"
            };
        }
    }
}
