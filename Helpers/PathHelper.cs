namespace BusTravelAccountsLambda;

public static class PathHelper
{
    public static string Normalize(string path) => string.IsNullOrWhiteSpace(path) ? "/" : (path.StartsWith('/') ? path : "/" + path);
    public static string StripBasePath(string requestPath, string basePath) => BasePath.Strip(requestPath, basePath);
}