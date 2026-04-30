using System.Threading.Tasks;
using azurefunction318.Models;

namespace azurefunction318.Repositories
{
    public interface ITravelcardRepository
    {
        Task InsertAsync(TravelcardRequest request);
    }
}
