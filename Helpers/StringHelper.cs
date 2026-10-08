namespace BusTravelAccountsSaMainLambda;

public static class StringHelper
{
    public static string? TrimOrNull(string? value)
    {
        return value?.Trim();
    }
}