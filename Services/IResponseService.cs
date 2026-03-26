using System.Threading.Tasks;

namespace ResponseHttp.Services
{
    /// <summary>
    /// Service contract for fetching and formatting a response from the configured HTTP connection.
    /// </summary>
    public interface IResponseService
    {
        /// <summary>
        /// Fetches the remote response and returns the concatenated result.
        /// </summary>
        Task<string> FetchAndFormatResponseAsync();
    }
}
