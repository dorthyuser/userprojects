using System.Threading.Tasks;
using travelcard_function_app.Models;

namespace travelcard_function_app.Services;

public class TravelcardService
{
    private readonly TravelcardRepository _repository;

    public TravelcardService(TravelcardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request)
    {
        var result = await _repository.InsertAsync(request);
        return new CreateTravelcardResponse
        {
            TravelcardId = result.TravelcardId.ToString(),
            Token = result.Token
        };
    }
}
