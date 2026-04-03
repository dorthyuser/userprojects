using System.Net.Http;
using System.Threading.Tasks;

namespace TravelcardService.Helpers
{
    public interface IHttpHelper
    {
        Task<HttpResponseMessage> PostToTravelcardAsync(string rawJson);
    }
}
