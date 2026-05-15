using System.Threading;
using System.Threading.Tasks;
using TravelcardDb.Models;

namespace TravelcardDb.Services;

public interface ITravelcardDbConnection
{
    Task<TravelcardForwardResult> ForwardAsync(string requestBody, CancellationToken cancellationToken = default);
}