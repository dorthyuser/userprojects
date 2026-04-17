using System.Net.Http;
using System.Threading.Tasks;

namespace ZohoOauthKvTest.Services
{
    public interface IZohoHttpService
    {
        Task<HttpResponseMessage> GetLeadsAsync();
    }
}
