using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TravelcardService.Helpers
{
    public interface ITravelcardHttpHelper
    {
        Task<string> ForwardAsync(string body, ILogger logger);
    }
}
