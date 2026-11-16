using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

public sealed class HealthService(ILogger<HealthService> logger) : IHealthService
{
    public Task<string> AliveAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Alive check");
        return Task.FromResult("UP");
    }

    public Task<string> ReadyAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Ready check");
        return Task.FromResult("UP");
    }
}
