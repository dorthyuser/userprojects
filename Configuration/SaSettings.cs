using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BusTravelAccountsLambda;

public sealed class SaSettings
{
    private static readonly object Sync = new();
    private static SaSettings? cached;
    public string? DbHost { get; }
    public int? DbPort { get; }
    public string? DbName { get; }
    public string? DbUsername { get; }
    public string? DbPassword { get; }
    public string? AwsSecretName { get; }

    private SaSettings(string? dbHost, int? dbPort, string? dbName, string? dbUsername, string? dbPassword, string? awsSecretName)
    {
        DbHost = dbHost;
        DbPort = dbPort;
        DbName = dbName;
        DbUsername = dbUsername;
        DbPassword = dbPassword;
        AwsSecretName = awsSecretName;
    }

    public static SaSettings Current()
    {
        lock (Sync)
        {
            if (cached is not null) return cached;
            var awsSecretName = Environment.GetEnvironmentVariable("AWS_SECRET_NAME");
            if (!string.IsNullOrWhiteSpace(awsSecretName))
            {
                var client = new AmazonSecretsManagerClient();
                var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = awsSecretName }, CancellationToken.None).GetAwaiter().GetResult();
                var secret = JsonDocument.Parse(response.SecretString ?? "{}").RootElement;
                string? ReadText(string name)
                {
                    if (!secret.TryGetProperty(name, out var value)) return null;
                    return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
                }
                cached = new SaSettings(ReadText("host"), int.TryParse(ReadText("port"), out var port) ? port : null, ReadText("dbname"), ReadText("username"), ReadText("password"), awsSecretName);
                return cached;
            }
            cached = new SaSettings(Environment.GetEnvironmentVariable("DB_HOST"), int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var port2) ? port2 : null, Environment.GetEnvironmentVariable("DB_NAME"), Environment.GetEnvironmentVariable("DB_USERNAME"), Environment.GetEnvironmentVariable("DB_PASSWORD"), awsSecretName);
            return cached;
        }
    }
}