namespace BusTravelAccountsSaFunction;

public interface IHealthService
{
    Task<string> AliveAsync(CancellationToken cancellationToken);

    Task<string> ReadyAsync(CancellationToken cancellationToken);
}
