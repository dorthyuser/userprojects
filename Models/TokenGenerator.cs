namespace travelcardlambdachsarp549.Models;

public static class TokenGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Random _random = new();

    public static string GenerateToken(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++) chars[i] = Alphabet[_random.Next(Alphabet.Length)];
        return new string(chars);
    }
}