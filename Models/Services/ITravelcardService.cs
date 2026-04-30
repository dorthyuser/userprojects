using azuresharpapi153.Models.Requests;
using azuresharpapi153.Models.Responses;

namespace azuresharpapi153.Services;

public interface ITravelcardService
{
    Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, string? correlationId, CancellationToken cancellationToken);
}