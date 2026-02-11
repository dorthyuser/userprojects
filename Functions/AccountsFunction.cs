using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AccountsFunction.Helpers;
using AccountsFunction.Models;

namespace AccountsFunction.Functions
{
 public class AccountsFunction
 {
 private readonly DatabaseHelper _dbHelper;
 private readonly ILogger<AccountsFunction> _logger;
 private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
 {
 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
 WriteIndented = true
 };

 public AccountsFunction(DatabaseHelper dbHelper, ILogger<AccountsFunction> logger)
 {
 _dbHelper = dbHelper;
 _logger = logger;
 }

 // GET /accounts/{id} - Id is mandatory
 [Function("GetAccountById")]
 public async Task<HttpResponseData> GetAccountById(
 [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{id}")] HttpRequestData req,
 string id)
 {
 try
 {
 if (string.IsNullOrWhiteSpace(id))
 {
 var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
 var err = new ErrorResponse { Code = "missing_id", Message = "The id path parameter is required.", Details = null };
 await WriteJsonAsync(badResp, err);
 return badResp;
 }

 if (!DatabaseHelper.TryParseGuid(id, out var guid))
 {
 var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
 var err = new ErrorResponse { Code = "invalid_id", Message = "The provided id is not a valid GUID.", Details = id };
 await WriteJsonAsync(badResp, err);
 return badResp;
 }

 var account = await _dbHelper.GetAccountByIdAsync(guid);
 if (account == null)
 {
 var notFound = req.CreateResponse(HttpStatusCode.NotFound);
 var err = new ErrorResponse { Code = "not_found", Message = "Account not found.", Details = id };
 await WriteJsonAsync(notFound, err);
 return notFound;
 }

 var okResp = req.CreateResponse(HttpStatusCode.OK);
 await WriteJsonAsync(okResp, account);
 return okResp;
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Unhandled exception in GetAccountById");
 var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
 var err = new ErrorResponse
 {
 Code = "internal_error",
 Message = "An unexpected error occurred while processing the request.",
 Details = ex.Message
 };
 await WriteJsonAsync(resp, err);
 return resp;
 }
 }

 // POST /accounts - create multiple accounts at once. Body must be JSON array of accounts or a single account object.
 // Each account must include id (GUID) - id is mandatory
 [Function("CreateAccounts")]
 public async Task<HttpResponseData> CreateAccounts(
 [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequestData req)
 {
 try
 {
 var body = await req.ReadAsStringAsync();
 if (string.IsNullOrWhiteSpace(body))
 {
 var bad = req.CreateResponse(HttpStatusCode.BadRequest);
 var err = new ErrorResponse { Code = "empty_body", Message = "Request body is empty.", Details = null };
 await WriteJsonAsync(bad, err);
 return bad;
 }

 List<Account>? accounts = null;
 try
 {
 // Try array first
 accounts = JsonSerializer.Deserialize<List<Account>>(body, _jsonOptions);
 if (accounts == null)
 {
 // Try single object
 var single = JsonSerializer.Deserialize<Account>(body, _jsonOptions);
 if (single != null)
 accounts = new List<Account> { single };
 }
 }
 catch (JsonException)
 {
 var bad = req.CreateResponse(HttpStatusCode.BadRequest);
 var err = new ErrorResponse { Code = "invalid_json", Message = "Request body is not valid JSON or does not match the account schema.", Details = null };
 await WriteJsonAsync(bad, err);
 return bad;
 }

 if (accounts == null || accounts.Count == 0)
 {
 var bad = req.CreateResponse(HttpStatusCode.BadRequest);
 var err = new ErrorResponse { Code = "invalid_body", Message = "No account data provided.", Details = null };
 await WriteJsonAsync(bad, err);
 return bad;
 }

 // Validate each account: id mandatory and must be valid GUID (Account.Id cannot be Guid.Empty)
 var invalids = new List<object>();
 foreach (var acc in accounts)
 {
 if (acc == null)
 {
 invalids.Add(new { reason = "null_account" });
 continue;
 }

 if (acc.Id == Guid.Empty)
 {
 invalids.Add(new { id = acc.Id, reason = "missing_or_empty_id" });
 continue;
 }
 }

 if (invalids.Count > 0)
 {
 var bad = req.CreateResponse(HttpStatusCode.BadRequest);
 var err = new ErrorResponse { Code = "invalid_accounts", Message = "One or more account items are invalid. Each account must include a non-empty GUID id.", Details = JsonSerializer.Serialize(invalids, _jsonOptions) };
 await WriteJsonAsync(bad, err);
 return bad;
 }

 // Create accounts
 var created = await _dbHelper.CreateAccountsAsync(accounts);

 var resp = req.CreateResponse(HttpStatusCode.Created);
 await WriteJsonAsync(resp, created);
 return resp;
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Unhandled exception in CreateAccounts");
 var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
 var err = new ErrorResponse
 {
 Code = "internal_error",
 Message = "An unexpected error occurred while processing the request.",
 Details = ex.Message
 };
 await WriteJsonAsync(resp, err);
 return resp;
 }
 }

 private async Task WriteJsonAsync(HttpResponseData response, object obj)
 {
 response.Headers.Add("Content-Type", "application/json; charset=utf-8");
 var json = JsonSerializer.Serialize(obj, _jsonOptions);
 await response.WriteStringAsync(json);
 }
 }
}
