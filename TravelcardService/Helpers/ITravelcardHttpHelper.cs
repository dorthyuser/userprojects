using System.Net.Http;
using System.Threading.Tasks;

namespace TravelcardService.Helpers
{
    public interface ITravelcardHttpHelper
    {
        Task<HttpResponseMessage> ForwardAsync(string body);
    }
}
