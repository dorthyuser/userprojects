using System.Threading.Tasks;

namespace TravelcardFunctionApp.Helpers
{
    public interface IHttpHelper
    {
        Task<string> GetAccessTokenAsync(string tokenUrl, string clientId, string clientSecret, string scope);
    }
}
