using System;

namespace BusTravelAccountsLambda;

public sealed class EaSettings
{
    public string ListenerPath { get; }

    public EaSettings()
    {
        ListenerPath = Environment.GetEnvironmentVariable("HTTPS_LISTENER_PATH") ?? "api/v1/*";
    }
}