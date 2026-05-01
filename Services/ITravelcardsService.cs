using demo_travelcard_paul.Models;

namespace demo_travelcard_paul.Services;

public interface ITravelcardsService
{
    Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request);
}