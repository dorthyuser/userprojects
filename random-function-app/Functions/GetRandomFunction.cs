using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RandomFunctionApp.Helpers;

namespace RandomFunctionApp.Functions
{
    public class GetRandomFunction
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<GetRandomFunction> _logger;
        private readonly IConfiguration _config;

        public GetRandomFunction(DbHelper dbHelper, ILogger<GetRandomFunction> logger, IConfiguration config)
        {
            _dbHelper = dbHelper;
            _logger = logger;
            _config = config;
        }

        [Function("GetRandom")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "random")] HttpRequestData req, FunctionContext context)
        {
            _logger.LogInformation("Entering GetRandom");
            try
            {
                if (!req.Headers.TryGetValues("x-api-key", out var apiKeys) || apiKeys == null)
                {
                    var resp401 = req.CreateResponse(HttpStatusCode.Unauthorized);
                    resp401.Headers.Add("Content-Type", "application/json");
                    var errMissing = new { error = new { message = "Missing API key" } };
                    await resp401.WriteStringAsync(JsonSerializer.Serialize(errMissing));
                    _logger.LogWarning("Missing API key");
                    _logger.LogInformation("Exiting GetRandom");
                    return resp401;
                }

                var providedKey = "";
                foreach (var v in apiKeys)
                {
                    providedKey = v;
                    break;
                }

                var configuredKey = _config["API_KEY"] ?? string.Empty;
                if (providedKey != configuredKey)
                {
                    var resp401 = req.CreateResponse(HttpStatusCode.Unauthorized);
                    resp401.Headers.Add("Content-Type", "application/json");
                    var errInvalid = new { error = new { message = "Invalid API key" } };
                    await resp401.WriteStringAsync(JsonSerializer.Serialize(errInvalid));
                    _logger.LogWarning("Invalid API key");
                    _logger.LogInformation("Exiting GetRandom");
                    return resp401;
                }

                var rnd = Random.Shared.Next(100, 201);

                await _dbHelper.InsertRandomValueAsync(rnd);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                var payload = new { value = rnd };
                await response.WriteStringAsync(JsonSerializer.Serialize(payload));

                _logger.LogInformation("Exiting GetRandom");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRandom");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json");
                var err = new { error = new { message = "Internal server error", details = ex.Message } };
                await response.WriteStringAsync(JsonSerializer.Serialize(err));
                return response;
            }
        }
    }
}
