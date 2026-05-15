using System.Threading;
using System.Threading.Tasks;
using TravelcardDb.Models;

namespace TravelcardDb.Services;

public interface ITravelcardDbService
{
    Task<TravelcardForwardResult> ForwardTravelcardAsync(string requestBody, CancellationToken cancellationToken = default);
}