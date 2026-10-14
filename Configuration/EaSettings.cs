using System;

namespace BusTravelAccountsLambda;

public sealed class EaSettings
{
    public string ListenerPath { get; }
    public string ApiVersion { get; }

    public EaSettings()
    {
        ApiVersion = Environment.GetEnvironmentVariable("API_VERSION") ?? "v1";
        ListenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? $"api/{ApiVersion}/*";
    }
}