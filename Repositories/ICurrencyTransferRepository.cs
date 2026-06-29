using currency_calculator.Models;

namespace currency_calculator.Repositories;

public interface ICurrencyTransferRepository
{
    List<SupportedCurrencyItemResponse> GetSupportedCurrencies();
    RatesResponse GetRates(string baseCurrency);
    ValidateResponse Validate(CurrencyTransferRequest request);
    CalculateResponse Calculate(CurrencyTransferRequest request);
}