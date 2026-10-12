namespace PrimeNumberService.Services;

public interface IPrimeNumberService
{
    Task<int> GetPrimeNumberBetween100And200Async(CancellationToken cancellationToken = default);
}