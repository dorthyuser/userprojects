using travelcardcsharpsb1114.Models;

namespace travelcardcsharpsb1114.Services;

public interface ITravelcardService
{
    Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, CancellationToken cancellationToken = default);
}