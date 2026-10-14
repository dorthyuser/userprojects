namespace hello.Services;

public interface IPrimeService
{
    Task<int> GetPrimeNumberBetween100And200Async(CancellationToken cancellationToken);
}
