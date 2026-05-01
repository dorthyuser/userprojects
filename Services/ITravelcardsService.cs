using azuretravelcardapi121.Models;

namespace azuretravelcardapi121.Services;

public interface ITravelcardsService
{
    Task<TravelcardResponse> CreateAsync(TravelcardCreateRequest request, CancellationToken cancellationToken);
}