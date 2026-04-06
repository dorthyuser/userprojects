using System.Threading.Tasks;

namespace TravelcardService.Helpers
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
    }
}
