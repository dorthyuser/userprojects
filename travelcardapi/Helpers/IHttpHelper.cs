using System.Net.Http;
using System.Threading.Tasks;
using TravelcardApi.Models;

namespace TravelcardApi.Helpers
{
    public interface IHttpHelper
    {
        Task<HttpResponseMessage> PostTravelcardAsync(TravelcardRequest request, string originalRequestJson);
    }
}
