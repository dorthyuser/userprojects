using System.Threading.Tasks;

namespace hello_http_test.Data
{
    /// <summary>
    /// Data access abstraction for Zoho store operations. Implementations call the external API.
    /// </summary>
    public interface IZohoStoreRepository
    {
        Task<string> GetStoresAsync();
        Task<string> CreateStoreAsync(object store);
        Task<string> UpdateStoreAsync(string id, object store);
    }
}
