using System.Threading.Tasks;

namespace tc_csharp_api
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
        Task ForceRefreshAsync();
    }
}
