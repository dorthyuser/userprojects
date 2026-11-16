using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusTravelAccountsSaFunction;

public sealed class SalesforceClient(HttpClient http, IOptions<SalesforceOptions> options, ILogger<SalesforceClient> logger) : ISalesforceClient
{
    private const string ApiVersion = "64.0";

    private sealed record Session(string Instance, string Token);

    private static Session? session;

    public async Task<IReadOnlyList<JsonObject>> QueryAsync(string soql, IReadOnlyDictionary<string, string?> parameters, CancellationToken cancellationToken)
    {
        var q = Uri.EscapeDataString(Bind(soql, parameters));
        var page = await SendAsync(s => new HttpRequestMessage(HttpMethod.Get, $"{Api(s)}/query?q={q}"), cancellationToken);
        var records = new List<JsonObject>();

        while (true)
        {
            foreach (var record in page["records"]!.AsArray())
            {
                records.Add(WithoutAttributes(record!.DeepClone().AsObject()));
            }

            if (page["done"]?.GetValue<bool>() == true || page["nextRecordsUrl"] is null)
            {
                break;
            }

            var next = page["nextRecordsUrl"]!.GetValue<string>();
            page = await SendAsync(s => new HttpRequestMessage(HttpMethod.Get, s.Instance + next), cancellationToken);
        }

        logger.LogInformation("Salesforce query: {Records} records", records.Count);
        return records;
    }

    public async Task<SalesforceBulkResult> CreateAsync(string type, IReadOnlyList<JsonObject> records, CancellationToken cancellationToken)
    {
        var items = new List<SalesforceBulkItem>();

        foreach (var chunk in records.Chunk(200))
        {
            var typed = new JsonArray();
            foreach (var record in chunk)
            {
                var copy = record.DeepClone().AsObject();
                copy["attributes"] = new JsonObject { ["type"] = type };
                typed.Add(copy);
            }

            var body = new JsonObject { ["allOrNone"] = false, ["records"] = typed };
            var answers = await SendAsync(s => new HttpRequestMessage(HttpMethod.Post, $"{Api(s)}/composite/sobjects")
            {
                Content = new StringContent(body.ToJsonString(JsonDefaults.Options), Encoding.UTF8, "application/json")
            }, cancellationToken);

            foreach (var answer in answers.AsArray())
            {
                items.Add(ToItem(answer!.AsObject()));
            }
        }

        logger.LogInformation("Salesforce create {Type}: {Records} records", type, records.Count);
        return new SalesforceBulkResult
        {
            Successful = items.All(i => i.Successful),
            Items = items
        };
    }

    public async Task<SalesforceBulkResult> UpdateAsync(string type, IReadOnlyList<JsonObject> records, CancellationToken cancellationToken)
    {
        var items = new List<SalesforceBulkItem>();

        foreach (var chunk in records.Chunk(200))
        {
            var typed = new JsonArray();
            foreach (var record in chunk)
            {
                var copy = record.DeepClone().AsObject();
                copy["attributes"] = new JsonObject { ["type"] = type };
                typed.Add(copy);
            }

            var body = new JsonObject { ["allOrNone"] = false, ["records"] = typed };
            var method = new HttpMethod("PATCH");
            var answers = await SendAsync(s => new HttpRequestMessage(method, $"{Api(s)}/composite/sobjects")
            {
                Content = new StringContent(body.ToJsonString(JsonDefaults.Options), Encoding.UTF8, "application/json")
            }, cancellationToken);

            foreach (var answer in answers.AsArray())
            {
                items.Add(ToItem(answer!.AsObject()));
            }
        }

        logger.LogInformation("Salesforce update {Type}: {Records} records", type, records.Count);
        return new SalesforceBulkResult
        {
            Successful = items.All(i => i.Successful),
            Items = items
        };
    }

    private static SalesforceBulkItem ToItem(JsonObject answer) => new()
    {
        Id = answer["id"]?.DeepClone(),
        Successful = answer["success"]?.GetValue<bool>() == true,
        Payload = answer.DeepClone().AsObject()
    };

    private async Task<JsonNode> SendAsync(Func<Session, HttpRequestMessage> build, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var current = session ?? await LoginAsync(cancellationToken);
            using var request = build(current);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.Token);
            HttpResponseMessage response;

            try
            {
                response = await http.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Salesforce request failed");
                throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Salesforce request timed out");
                throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
            }

            using (response)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    session = null;
                    if (attempt == 1)
                    {
                        continue;
                    }

                    throw new SalesforceException("SALESFORCE", "INVALID_SESSION", "The Salesforce session was refused");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw SalesforceException.Of((int)response.StatusCode, text, logger);
                }

                return text.Length == 0 ? new JsonObject() : JsonNode.Parse(text)!;
            }
        }
    }

    private async Task<Session> LoginAsync(CancellationToken cancellationToken)
    {
        var o = options.Value;
        var url = new Uri(o.LoginUrl);
        var sandbox = url.Host == "test.salesforce.com" || url.Host.EndsWith(".sandbox.my.salesforce.com", StringComparison.OrdinalIgnoreCase);
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object> { ["alg"] = "RS256" }, JsonDefaults.Options));
        var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["iss"] = o.ConsumerKey,
            ["sub"] = o.Username,
            ["aud"] = sandbox ? "https://test.salesforce.com" : "https://login.salesforce.com",
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds()
        }, JsonDefaults.Options));
        var unsigned = $"{header}.{claims}";

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = $"{unsigned}.{Sign(unsigned, o.PrivateKey)}"
        });

        try
        {
            using var response = await http.PostAsync($"{url.GetLeftPart(UriPartial.Authority)}/services/oauth2/token", form, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var reason = Refusal(text);
                logger.LogWarning("Salesforce login refused ({Status}): {Reason}", (int)response.StatusCode, reason);
                throw new SalesforceException("SALESFORCE", "CONNECTIVITY", $"Invalid login: {reason}");
            }

            using var answer = JsonDocument.Parse(text);
            var instance = new Uri(answer.RootElement.GetProperty("instance_url").GetString()!);
            logger.LogInformation("Salesforce: logged in to {Instance}", instance.Host);
            var fresh = new Session(instance.GetLeftPart(UriPartial.Authority), answer.RootElement.GetProperty("access_token").GetString()!);
            session = fresh;
            return fresh;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Salesforce login failed");
            throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Salesforce login timed out");
            throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
        }
    }

    private string Refusal(string body)
    {
        try
        {
            using var answer = JsonDocument.Parse(body);
            var root = answer.RootElement;
            var error = root.TryGetProperty("error", out var e) ? e.GetString() : string.Empty;
            var description = root.TryGetProperty("error_description", out var d) ? d.GetString() : string.Empty;
            return $"{error} — {description}";
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Salesforce token answer is not JSON");
            return "the answer is not JSON";
        }
    }

    private static string Bind(string soql, IReadOnlyDictionary<string, string?> parameters) =>
        Regex.Replace(soql, @":(\w+)", m => parameters.TryGetValue(m.Groups[1].Value, out var value) ? EscapeSoql(value ?? string.Empty) : string.Empty);

    private static string EscapeSoql(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);

    private static JsonObject WithoutAttributes(JsonObject record)
    {
        record.Remove("attributes");
        foreach (var (_, value) in record)
        {
            if (value is JsonObject nested)
            {
                WithoutAttributes(nested);
            }
        }

        return record;
    }

    private static string Sign(string unsigned, string pem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem.Replace("\\n", "\n", StringComparison.Ordinal));
        return Base64Url(rsa.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Api(Session s) => $"{s.Instance}/services/data/v{ApiVersion}";
}
