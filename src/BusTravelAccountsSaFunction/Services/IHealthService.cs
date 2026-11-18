using Microsoft.AspNetCore.Mvc;

namespace BusTravelAccountsSaFunction;

public interface IHealthService
{
    Task<IActionResult> AliveAsync(CancellationToken cancellationToken);
    Task<IActionResult> ReadyAsync(CancellationToken cancellationToken);
}