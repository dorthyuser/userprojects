using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AgeApi.Helpers;
using AgeApi.Services;
using AgeApi.Models;
using AgeApi.Data;
using AgeApi.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace AgeApi.Functions
{
    public class AgeFunction
    {
        private readonly ILogger _logger;
        private readonly IAuthService _authService;
        private readonly AppDbContext _dbContext;
        private readonly JsonSerializerOptions _jsonOptions;

        public AgeFunction(ILoggerFactory loggerFactory, IAuthService authService, AppDbContext dbContext, JsonSerializerOptions jsonOptions)
        {
            _logger = loggerFactory.CreateLogger<AgeFunction>();
            _authService = authService;
            _dbContext = dbContext;
            _jsonOptions = jsonOptions;
        }

        [Function("GetAge")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "age")] HttpRequestData req, FunctionContext context)
        {
            try
            {
                // Authenticate
                if (!req.Headers.TryGetValues("x-api-key", out var keys))
                {
                    return await CreateErrorResponse(req, System.Net.HttpStatusCode.Unauthorized, "Missing API key", "Unauthorized");
                }

                var apiKey = string.Empty;
                foreach (var k in keys) { apiKey = k; break; }
                if (!_authService.ValidateApiKey(apiKey))
                {
                    return await CreateErrorResponse(req, System.Net.HttpStatusCode.Unauthorized, "Invalid API key", "Unauthorized");
                }

                // Parse query string
                var query = req.Url.Query ?? string.Empty;
                var q = ValidationHelper.ParseQuery(query);

                q.TryGetValue("dob", out var dobVal);
                if (string.IsNullOrWhiteSpace(dobVal))
                {
                    return await CreateErrorResponse(req, System.Net.HttpStatusCode.BadRequest, "Missing required query parameter 'dob'", "BadRequest", new System.Collections.Generic.Dictionary<string, string?> { { "parameter", "dob" } });
                }

                if (!DateHelper.TryParseDob(dobVal, out var dobUtc))
                {
                    return await CreateErrorResponse(req, System.Net.HttpStatusCode.BadRequest, "Invalid dob format. Use yyyy-MM-dd or ISO 8601.", "BadRequest", new System.Collections.Generic.Dictionary<string, string?> { { "dob", dobVal } });
                }

                var nowUtc = DateTime.UtcNow;
                if (dobUtc > nowUtc)
                {
                    return await CreateErrorResponse(req, System.Net.HttpStatusCode.BadRequest, "The provided dob is in the future.", "BadRequest", new System.Collections.Generic.Dictionary<string, string?> { { "dob", dobVal } });
                }

                var (days, weeks, minutes, seconds) = DateHelper.CalculateAgeComponents(dobUtc, nowUtc);

                var response = new AgeResponse
                {
                    Days = days,
                    Weeks = weeks,
                    Minutes = minutes,
                    Seconds = seconds,
                    Summary = $"{days} days, {weeks} weeks, {minutes} minutes, {seconds} seconds"
                };

                // Save request metadata to in-memory DB for auditing
                try
                {
                    var entity = new UserEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = null,
                        DateOfBirth = dobUtc,
                        RequestedAt = nowUtc
                    };
                    _dbContext.Users.Add(entity);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save request metadata to DB");
                }

                var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
                res.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await res.WriteStringAsync(JsonSerializer.Serialize(response, _jsonOptions));
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetAge function");
                return await CreateErrorResponse(req, System.Net.HttpStatusCode.InternalServerError, "An unexpected error occurred.", "ServerError", null, ex.Message);
            }
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, System.Net.HttpStatusCode statusCode, string message, string code, System.Collections.Generic.IDictionary<string, string?>? details = null, string? internalMessage = null)
        {
            var error = new ErrorResponse
            {
                Message = message,
                Code = code,
                Details = details ?? new System.Collections.Generic.Dictionary<string, string?>()
            };

            if (!string.IsNullOrEmpty(internalMessage))
            {
                error.Details["internal"] = internalMessage;
            }

            var res = req.CreateResponse(statusCode);
            res.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await res.WriteStringAsync(JsonSerializer.Serialize(error, _jsonOptions));
            return res;
        }
    }
}
