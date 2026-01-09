using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AccountsFunction.Helpers;
using AccountsFunction.Models;
using System.Collections.Generic;

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

 // Only implement GET because the user did not explicitly request other HTTP methods
 [Function("GetAccounts")]
 public async Task<HttpResponseData> GetAccounts(
 [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{id?}")] HttpRequestData req,
 string id)
 {
 try
 {
 if (!string.IsNullOrWhiteSpace(id))
 {
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
 else
 {
 var accounts = await _dbHelper.GetAllAccountsAsync();
 var okResp = req.CreateResponse(HttpStatusCode.OK);
 await WriteJsonAsync(okResp, accounts);
 return okResp;
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Unhandled exception in GetAccounts");
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
