using System.Collections.Generic;
using System.Threading.Tasks;

namespace tc_csharp_api
{
    public interface ISecretProvider
    {
        Task<IDictionary<string, string>> GetSecretAsync(string secretName);
    }
}
