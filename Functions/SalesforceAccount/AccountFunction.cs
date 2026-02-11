using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SalesforceAccountFunctions.Helpers.Salesforce;
using SalesforceAccountFunctions.Models;
using SalesforceAccountFunctions.Helpers.Logging;

namespace SalesforceAccountFunctions.Functions.SalesforceAccount
{
    public class AccountFunction
    {
        private readonly ISalesforceClient _sfClient;
        private readonly ILogger _logger;
        private readonly string _apiKey;

        public AccountFunction(ISalesforceClient sfClient, ILoggerFactory loggerFactory)
        {
            _sfClient = sfClient ?? throw new ArgumentNullException(nameof(sfClient));
            _logger = loggerFactory.CreateLogger<AccountFunction>();
            _apiKey = Environment.GetEnvironmentVariable("FUNCTION_API_KEY") ?? string.Empty;
        }

        [Function("GetAccount")]
        public async Task<HttpResponseData> GetAccount([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts/{id}")] HttpRequestData req, string id)
        {
            try
            {
                if (!ValidateApiKey(req))
                {
                    return CreateUnauthorized(req, "Invalid API Key");
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    return CreateBadRequest(req, "id is required");
                }

                var result = await _sfClient.GetAccountAsync(id);
                if (result == null)
                {
                    return CreateNotFound(req, $"Account with id '{id}' not found");
                }

                var resp = req.CreateResponse(HttpStatusCode.OK);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(result, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions));
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAccount");
                return CreateErrorResponse(req, "SF_GET_ERROR", "Failed to get account", ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("CreateAccount")]
        public async Task<HttpResponseData> CreateAccount([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "accounts")] HttpRequestData req)
        {
            try
            {
                if (!ValidateApiKey(req))
                {
                    return CreateUnauthorized(req, "Invalid API Key");
                }

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return CreateBadRequest(req, "Request body is required");
                }

                AccountDto? input;
                try
                {
                    input = JsonSerializer.Deserialize<AccountDto>(body, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid JSON payload");
                    return CreateBadRequest(req, "Invalid JSON payload");
                }

                if (input == null || string.IsNullOrWhiteSpace(input.Name))
                {
                    return CreateBadRequest(req, "Account 'name' is required");
                }

                var createResult = await _sfClient.CreateAccountAsync(input);
                if (!createResult.Success)
                {
                    return CreateErrorResponse(req, "SF_CREATE_ERROR", "Failed to create account", createResult.ErrorDetails ?? "Unknown", HttpStatusCode.BadRequest);
                }

                var resp = req.CreateResponse(HttpStatusCode.Created);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(new { id = createResult.Id, success = true }, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions));
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateAccount");
                return CreateErrorResponse(req, "SF_CREATE_ERROR", "Failed to create account", ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("DeleteAccount")]
        public async Task<HttpResponseData> DeleteAccount([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "accounts/{id}")] HttpRequestData req, string id)
        {
            try
            {
                if (!ValidateApiKey(req))
                {
                    return CreateUnauthorized(req, "Invalid API Key");
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    return CreateBadRequest(req, "id is required");
                }

                var deleted = await _sfClient.DeleteAccountAsync(id);
                if (!deleted.Success)
                {
                    return CreateErrorResponse(req, "SF_DELETE_ERROR", "Failed to delete account", deleted.ErrorDetails ?? "Unknown", HttpStatusCode.BadRequest);
                }

                var resp = req.CreateResponse(HttpStatusCode.OK);
                resp.Headers.Add("Content-Type", "application/json");
                await resp.WriteStringAsync(JsonSerializer.Serialize(new { id = id, deleted = true }, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions));
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAccount");
                return CreateErrorResponse(req, "SF_DELETE_ERROR", "Failed to delete account", ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private bool ValidateApiKey(HttpRequestData req)
        {
            if (string.IsNullOrEmpty(_apiKey)) return false;
            if (!req.Headers.TryGetValues("x-api-key", out var values)) return false;
            foreach (var v in values)
            {
                if (v == _apiKey) return true;
            }
            return false;
        }

        private HttpResponseData CreateBadRequest(HttpRequestData req, string message)
        {
            return CreateErrorResponse(req, "BAD_REQUEST", message, message, HttpStatusCode.BadRequest);
        }

        private HttpResponseData CreateUnauthorized(HttpRequestData req, string message)
        {
            return CreateErrorResponse(req, "UNAUTHORIZED", message, message, HttpStatusCode.Unauthorized);
        }

        private HttpResponseData CreateNotFound(HttpRequestData req, string message)
        {
            return CreateErrorResponse(req, "NOT_FOUND", message, message, HttpStatusCode.NotFound);
        }

        private HttpResponseData CreateErrorResponse(HttpRequestData req, string code, string message, string details, HttpStatusCode statusCode)
        {
            var resp = req.CreateResponse(statusCode);
            resp.Headers.Add("Content-Type", "application/json");
            var err = new ErrorResponse { Error = new ErrorDetail { Code = code, Message = message, Details = details } };
            resp.WriteString(JsonSerializer.Serialize(err, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions));
            return resp;
        }
    }
}
