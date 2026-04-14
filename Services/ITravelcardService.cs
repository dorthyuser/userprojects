using System.Net.Http;
using System.Threading.Tasks;

namespace tc_csharp_api
{
    public interface ITravelcardService
    {
        Task<HttpResponseMessage> ForwardAsync(string rawBody);
    }
}
