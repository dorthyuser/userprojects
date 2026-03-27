using System;
using System.Linq;

namespace TravelcardFunction.Helpers
{
    public static class TokenGenerator
    {
        private static readonly char[] _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        public static string GenerateToken(int length)
        {
            var rng = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => _chars[rng.Next(_chars.Length)]).ToArray());
        }
    }
}
