using azuretravelcardapi907.Models;

namespace azuretravelcardapi907.Services;

public interface ITravelcardService
{
    Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, string? correlationId, CancellationToken cancellationToken);
}