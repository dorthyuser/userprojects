using System.Threading.Tasks;
using TravelcardGatewayService.Models;

namespace TravelcardGatewayService.Helpers
{
    public interface ITravelcardHttpClient
    {
        Task<TravelcardResponse> ForwardTravelcardAsync(TravelcardRequest request);
    }
}
