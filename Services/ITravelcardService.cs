using System.Threading.Tasks;
using azurefunction318.Models;

namespace azurefunction318.Services
{
    public interface ITravelcardService
    {
        Task<TravelcardResponse> CreateAsync(TravelcardRequest request, string clientId, string correlationId);
    }
}
