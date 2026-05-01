using System;

namespace azuretravelcardfunction309.Helpers;

public static class RandomTokenGenerator
{
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Generate(int length)
    {
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = Characters[Random.Shared.Next(Characters.Length)];
        }

        return new string(result);
    }
}
