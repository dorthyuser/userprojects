using System.Collections.Generic;
using System.Threading.Tasks;
using hello_http_test.Models;

namespace hello_http_test.Services
{
    public interface IZohoService
    {
        Task<IEnumerable<StoreModel>> GetStoresAsync();
    }
}
