using System;

namespace TravelcardFunction.Helpers
{
    public static class TokenGenerator
    {
        private static readonly Random _rnd = new Random();
        public static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var buffer = new char[length];
            for (int i = 0; i < length; i++) buffer[i] = chars[_rnd.Next(chars.Length)];
            return new string(buffer);
        }
    }
}
