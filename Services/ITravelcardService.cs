using new_test_for_demo.Models;

namespace new_test_for_demo.Services;

public interface ITravelcardService
{
    Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request);
}