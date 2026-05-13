using System.Threading;
using System.Threading.Tasks;
using httptestingapi.Models;

namespace httptestingapi.Services;

public interface ITravelcardService
{
    Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, CancellationToken cancellationToken = default);
}
