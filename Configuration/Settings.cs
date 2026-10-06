using System;

namespace BusTravelAccountsEaMainFunction;

public sealed class Settings
{
    public string ListenerPath => GetRequired("HTTPS_LISTENER_PATH");
    public string AccountsHost => GetRequired("HTTPS_REQUESTER_SA_ACCOUNTS_HOST");
    public int AccountsPort => int.Parse(GetRequired("HTTPS_REQUESTER_SA_ACCOUNTS_PORT"));
    public string AccountsBasePath => GetRequired("HTTPS_REQUESTER_SA_ACCOUNTS_BASEPATH");
    public TimeSpan AccountsResponseTimeout => TimeSpan.FromMilliseconds(int.Parse(GetRequired("HTTPS_REQUESTER_SA_ACCOUNTS_RESPONSE_TIMEOUT")));
    public string AccountsClientId => GetRequired("HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_ID");
    public string AccountsClientSecret => GetRequired("HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_SECRET");

    private static string GetRequired(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? string.Empty;
    }
}