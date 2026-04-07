using System.Threading.Tasks;

namespace TravelcardService.Helpers
{
    public interface IKeyVaultHelper
    {
        Task<string?> GetSecretAsync(string name);
    }
}
