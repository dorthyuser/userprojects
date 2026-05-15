using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TravelcardDb.Models;

namespace TravelcardDb.Services;

public sealed class TravelcardDbService : ITravelcardDbService
{
    private readonly ITravelcardDbConnection _connection;
    private readonly ILogger<TravelcardDbService> _logger;

    public TravelcardDbService(ITravelcardDbConnection connection, ILogger<TravelcardDbService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<TravelcardForwardResult> ForwardTravelcardAsync(string requestBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[TravelcardDbService] Entering ForwardTravelcardAsync");
        try
        {
            var result = await _connection.ForwardAsync(requestBody, cancellationToken);
            _logger.LogInformation("[TravelcardDbService] Exiting ForwardTravelcardAsync with status {StatusCode}", result.StatusCode);
            return result;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[TravelcardDbService] ForwardTravelcardAsync failed: {Message}", ex.Message);
            throw;
        }
    }
}