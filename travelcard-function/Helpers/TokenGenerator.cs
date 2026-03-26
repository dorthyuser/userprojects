using System;

namespace travelcard_function.Helpers
{
    public static class TokenGenerator
    {
        public static string Generate(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            var buffer = new char[length];
            for (var i = 0; i < length; i++) buffer[i] = chars[rng.Next(chars.Length)];
            return new string(buffer);
        }
    }
}
