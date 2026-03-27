using System.Threading.Tasks;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public interface IDbHelper
    {
        Task<int> InsertTravelcardAsync(TravelcardRequest request, string token);
    }
}
