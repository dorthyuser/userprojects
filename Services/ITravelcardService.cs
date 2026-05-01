using azuretravelcardapi1218.Models;

namespace azuretravelcardapi1218.Services;

public interface ITravelcardService
{
    Task<TravelcardCreateResponse> CreateAsync(TravelcardCreateRequest request, string? correlationId, CancellationToken cancellationToken);
}