using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using test_sf_git_prop.Models;

namespace test_sf_git_prop.Services;

public sealed class SalesforceClient : ISalesforceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly SalesforceOptions _options;
    private readonly ILogger<SalesforceClient> _logger;
    private string? _accessToken;
    private string? _instanceUrl;

    public SalesforceClient(HttpClient httpClient, IOptions<SalesforceOptions> options, ILogger<SalesforceClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);
        var response = await _httpClient.GetAsync($"{_instanceUrl}/services/data/{_options.ApiVersion}/query?q={Uri.EscapeDataString("SELECT Id, Name, AccountNumber, Phone, Website, Industry, BillingStreet, BillingCity, BillingState, BillingPostalCode, BillingCountry FROM Account")}", cancellationToken);
        await EnsureSuccessAsync(response, "retrieve accounts");
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        var records = document.RootElement.GetProperty("records").EnumerateArray();
        return records.Select(MapAccount).ToArray();
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);
        var content = new StringContent(JsonSerializer.Serialize(MapCreateRequest(request), JsonOptions), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_instanceUrl}/services/data/{_options.ApiVersion}/sobjects/Account", content, cancellationToken);
        await EnsureSuccessAsync(response, "create account");
        var location = response.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(location))
        {
            return new AccountDto { Name = request.Name };
        }
        var getResponse = await _httpClient.GetAsync(location, cancellationToken);
        await EnsureSuccessAsync(getResponse, "retrieve created account");
        var payload = await getResponse.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        return MapAccount(document.RootElement);
    }

    public async Task<AccountDto> UpdateAccountAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);
        var payload = JsonSerializer.Serialize(MapUpdateRequest(request), JsonOptions);
        var message = new HttpRequestMessage(HttpMethod.Patch, $"{_instanceUrl}/services/data/{_options.ApiVersion}/sobjects/Account/{id}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, "update account");
        var getResponse = await _httpClient.GetAsync($"{_instanceUrl}/services/data/{_options.ApiVersion}/sobjects/Account/{id}", cancellationToken);
        await EnsureSuccessAsync(getResponse, "retrieve updated account");
        var content = await getResponse.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(content);
        return MapAccount(document.RootElement);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && !string.IsNullOrWhiteSpace(_instanceUrl))
        {
            return;
        }

        var loginUrl = _options.UseSandbox ? _options.SandboxLoginUrl : _options.ProductionLoginUrl;
        using var loginClient = new HttpClient();

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = _options.Username,
            ["password"] = _options.Password + _options.SecurityToken
        };

        var response = await loginClient.PostAsync($"{loginUrl}/services/oauth2/token", new FormUrlEncodedContent(form), cancellationToken);
        await EnsureSuccessAsync(response, "authenticate with Salesforce");
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        _accessToken = document.RootElement.GetProperty("access_token").GetString();
        _instanceUrl = document.RootElement.GetProperty("instance_url").GetString();
        _httpClient.BaseAddress = new Uri(_instanceUrl!);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    private static object MapCreateRequest(CreateAccountRequest request) => new
    {
        Name = request.Name,
        AccountNumber = request.AccountNumber,
        Phone = request.Phone,
        Website = request.Website,
        Industry = request.Industry,
        BillingStreet = request.BillingStreet,
        BillingCity = request.BillingCity,
        BillingState = request.BillingState,
        BillingPostalCode = request.BillingPostalCode,
        BillingCountry = request.BillingCountry
    };

    private static object MapUpdateRequest(UpdateAccountRequest request) => new
    {
        Name = request.Name,
        AccountNumber = request.AccountNumber,
        Phone = request.Phone,
        Website = request.Website,
        Industry = request.Industry,
        BillingStreet = request.BillingStreet,
        BillingCity = request.BillingCity,
        BillingState = request.BillingState,
        BillingPostalCode = request.BillingPostalCode,
        BillingCountry = request.BillingCountry
    };

    private static AccountDto MapAccount(JsonElement element) => new()
    {
        Id = element.TryGetProperty("Id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
        Name = element.TryGetProperty("Name", out var name) ? name.GetString() : null,
        AccountNumber = element.TryGetProperty("AccountNumber", out var accountNumber) ? accountNumber.GetString() : null,
        Phone = element.TryGetProperty("Phone", out var phone) ? phone.GetString() : null,
        Website = element.TryGetProperty("Website", out var website) ? website.GetString() : null,
        Industry = element.TryGetProperty("Industry", out var industry) ? industry.GetString() : null,
        BillingStreet = element.TryGetProperty("BillingStreet", out var billingStreet) ? billingStreet.GetString() : null,
        BillingCity = element.TryGetProperty("BillingCity", out var billingCity) ? billingCity.GetString() : null,
        BillingState = element.TryGetProperty("BillingState", out var billingState) ? billingState.GetString() : null,
        BillingPostalCode = element.TryGetProperty("BillingPostalCode", out var billingPostalCode) ? billingPostalCode.GetString() : null,
        BillingCountry = element.TryGetProperty("BillingCountry", out var billingCountry) ? billingCountry.GetString() : null
    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Failed to {operation}. Status code: {(int)response.StatusCode}. Response: {body}");
    }
}