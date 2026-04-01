using System.Threading.Tasks;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public interface ITravelcardService
    {
        Task<TravelcardResponse> PostTravelcardAsync(TravelcardRequest request);
    }
}
