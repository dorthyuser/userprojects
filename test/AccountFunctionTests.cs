using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SalesforceAccountFunctions.Functions.SalesforceAccount;
using SalesforceAccountFunctions.Helpers.Salesforce;
using SalesforceAccountFunctions.Models;
using SalesforceAccountFunctions.UnitTests.Helpers;
using Xunit;

namespace SalesforceAccountFunctions.UnitTests
{
    public class AccountFunctionTests
    {
        private readonly Mock<ISalesforceClient> _sfMock = new();
        private readonly Mock<FunctionContext> _funcContextMock = new();
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

        private Mock<HttpRequestData> CreateRequest(string? body = null, (string, string)[]? headers = null)
        {
            var reqMock = new Mock<HttpRequestData>(_funcContextMock.Object);

            // Body
            var stream = new MemoryStream();
            if (!string.IsNullOrEmpty(body))
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
            }
            stream.Seek(0, SeekOrigin.Begin);
            reqMock.Setup(r => r.Body).Returns(stream);

            // Headers
            var headersCollection = new HttpHeadersCollection();
            if (headers != null)
            {
                foreach (var (k, v) in headers)
                {
                    headersCollection.Add(k, v);
                }
            }
            reqMock.Setup(r => r.Headers).Returns(headersCollection);

            // CreateResponse => returns TestHttpResponseData so tests can inspect body/status
            reqMock
                .Setup(r => r.CreateResponse(It.IsAny<HttpStatusCode>()))
                .Returns((HttpStatusCode code) => new TestHttpResponseData(_funcContextMock.Object, code));

            return reqMock;
        }

        private AccountFunction CreateFunction()
        {
            // Ensure default API key is set for tests; tests will override env var as needed
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            return new AccountFunction(_sfMock.Object, _loggerFactory);
        }

        [Fact]
        public async Task GetAccount_InvalidApiKey_ReturnsUnauthorized()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "expected-key");
            var func = CreateFunction();
            var req = CreateRequest().Object; // no header

            var resp = await func.GetAccount(req, "abc");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("UNAUTHORIZED", err?.Error.Code);
        }

        [Fact]
        public async Task GetAccount_EmptyId_ReturnsBadRequest()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.GetAccount(req, string.Empty);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("BAD_REQUEST", err?.Error.Code);
        }

        [Fact]
        public async Task GetAccount_NotFound_ReturnsNotFound()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            _sfMock.Setup(s => s.GetAccountAsync("missing-id")).ReturnsAsync((AccountDto?)null);
            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.GetAccount(req, "missing-id");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("NOT_FOUND", err?.Error.Code);
        }

        [Fact]
        public async Task GetAccount_Success_ReturnsAccount()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var account = new AccountDto { Id = "001X", Name = "Acme", Phone = "123" };
            _sfMock.Setup(s => s.GetAccountAsync("001X")).ReturnsAsync(account);

            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.GetAccount(req, "001X");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var returned = JsonSerializer.Deserialize<AccountDto>(body, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions);
            Assert.NotNull(returned);
            Assert.Equal(account.Id, returned?.Id);
            Assert.Equal(account.Name, returned?.Name);
        }

        [Fact]
        public async Task GetAccount_Exception_ReturnsInternalError()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            _sfMock.Setup(s => s.GetAccountAsync(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("boom"));
            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.GetAccount(req, "any");
            Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("SF_GET_ERROR", err?.Error.Code);
        }

        [Fact]
        public async Task CreateAccount_InvalidApiKey_ReturnsUnauthorized()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "expected");
            var func = CreateFunction();
            var req = CreateRequest(body: JsonSerializer.Serialize(new { name = "x" })).Object;

            var resp = await func.CreateAccount(req);
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("UNAUTHORIZED", err?.Error.Code);
        }

        [Fact]
        public async Task CreateAccount_EmptyBody_ReturnsBadRequest()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var func = CreateFunction();
            var req = CreateRequest(body: string.Empty, headers: new[] { ("x-api-key", "test-key") }).Object;

            var resp = await func.CreateAccount(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("BAD_REQUEST", err?.Error.Code);
        }

        [Fact]
        public async Task CreateAccount_InvalidJson_ReturnsBadRequest()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var func = CreateFunction();
            var req = CreateRequest(body: "{ invalid json", headers: new[] { ("x-api-key", "test-key") }).Object;

            var resp = await func.CreateAccount(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("BAD_REQUEST", err?.Error.Code);
        }

        [Fact]
        public async Task CreateAccount_MissingName_ReturnsBadRequest()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var func = CreateFunction();
            var req = CreateRequest(body: JsonSerializer.Serialize(new { phone = "x" }), headers: new[] { ("x-api-key", "test-key") }).Object;

            var resp = await func.CreateAccount(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("BAD_REQUEST", err?.Error.Code);
        }

        [Fact]
        public async Task CreateAccount_SalesforceFailure_ReturnsBadRequestWithSfCode()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var input = new AccountDto { Name = "x" };
            _sfMock.Setup(s => s.CreateAccountAsync(It.IsAny<AccountDto>()))
                   .ReturnsAsync(new SalesforceCreateResult { Success = false, ErrorDetails = "sf-error" });

            var func = CreateFunction();
            var req = CreateRequest(body: JsonSerializer.Serialize(input, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions), headers: new[] { ("x-api-key", "test-key") }).Object;

            var resp = await func.CreateAccount(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("SF_CREATE_ERROR", err?.Error.Code);
        }

        [Fact]
        public async Task CreateAccount_Success_ReturnsCreatedWithId()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var input = new AccountDto { Name = "x" };
            _sfMock.Setup(s => s.CreateAccountAsync(It.IsAny<AccountDto>()))
                   .ReturnsAsync(new SalesforceCreateResult { Success = true, Id = "001ABC" });

            var func = CreateFunction();
            var req = CreateRequest(body: JsonSerializer.Serialize(input, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions), headers: new[] { ("x-api-key", "test-key") }).Object;

            var resp = await func.CreateAccount(req);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("id", out var idEl));
            Assert.Equal("001ABC", idEl.GetString());
            Assert.True(root.TryGetProperty("success", out var sEl));
            Assert.True(sEl.GetBoolean());
        }

        [Fact]
        public async Task CreateAccount_Exception_ReturnsInternalError()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            _sfMock.Setup(s => s.CreateAccountAsync(It.IsAny<AccountDto>())).ThrowsAsync(new Exception("boom"));
            var func = CreateFunction();
            var req = CreateRequest(body: JsonSerializer.Serialize(new AccountDto { Name = "x" }, SalesforceAccountFunctions.Helpers.Salesforce.SalesforceClient.JsonOptions), headers: new[] { ("x-api-key", "test-key") }).Object;

            var resp = await func.CreateAccount(req);
            Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("SF_CREATE_ERROR", err?.Error.Code);
        }

        [Fact]
        public async Task DeleteAccount_InvalidApiKey_ReturnsUnauthorized()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "expected");
            var func = CreateFunction();
            var req = CreateRequest().Object;

            var resp = await func.DeleteAccount(req, "id");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("UNAUTHORIZED", err?.Error.Code);
        }

        [Fact]
        public async Task DeleteAccount_EmptyId_ReturnsBadRequest()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.DeleteAccount(req, string.Empty);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("BAD_REQUEST", err?.Error.Code);
        }

        [Fact]
        public async Task DeleteAccount_SalesforceFailure_ReturnsBadRequest()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            _sfMock.Setup(s => s.DeleteAccountAsync("xid")).ReturnsAsync(new SalesforceDeleteResult { Success = false, ErrorDetails = "err" });
            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.DeleteAccount(req, "xid");
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("SF_DELETE_ERROR", err?.Error.Code);
        }

        [Fact]
        public async Task DeleteAccount_Success_ReturnsOk()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            _sfMock.Setup(s => s.DeleteAccountAsync("xid")).ReturnsAsync(new SalesforceDeleteResult { Success = true });
            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.DeleteAccount(req, "xid");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.Equal("xid", root.GetProperty("id").GetString());
            Assert.True(root.GetProperty("deleted").GetBoolean());
        }

        [Fact]
        public async Task DeleteAccount_Exception_ReturnsInternalError()
        {
            Environment.SetEnvironmentVariable("FUNCTION_API_KEY", "test-key");
            _sfMock.Setup(s => s.DeleteAccountAsync(It.IsAny<string>())).ThrowsAsync(new Exception("boom"));
            var req = CreateRequest(headers: new[] { ("x-api-key", "test-key") }).Object;
            var func = CreateFunction();

            var resp = await func.DeleteAccount(req, "id");
            Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            var body = await ((TestHttpResponseData)resp).ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<Helpers.Logging.ErrorResponse>(body);
            Assert.Equal("SF_DELETE_ERROR", err?.Error.Code);
        }
    }
}
