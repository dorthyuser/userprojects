namespace BusTravelAccountsSaLambda;

public static class PathHelper
{
    public static bool IsAccountById(string path, out string id)
    {
        id = string.Empty;
        if (!path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        id = parts[1];
        return true;
    }
}