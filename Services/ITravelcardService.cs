using System.Net.Http;
using System.Threading.Tasks;

namespace tc_testing_api2.Services
{
    /// <summary>
    /// Service contract for forwarding travelcard requests to the upstream API.
    /// </summary>
    public interface ITravelcardService
    {
        Task<HttpResponseMessage> ForwardAsync(string body);
    }
}