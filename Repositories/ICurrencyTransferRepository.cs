namespace currency_calculator.Repositories;

public interface ICurrencyTransferRepository
{
    Task<(decimal MarketRate, decimal BankRate, bool Cached, int CacheTtlSeconds, DateTime Timestamp)> GetRatesAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);
}