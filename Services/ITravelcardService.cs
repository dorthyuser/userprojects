using demo_travelcard_aus.Models;

namespace demo_travelcard_aus.Services;

public interface ITravelcardService
{
    Task<TravelcardResponse> CreateAsync(TravelcardCreateRequest request, CancellationToken cancellationToken);
}