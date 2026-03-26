using System;
using System.Linq;

namespace TravelcardFunction.Models
{
    public static class TokenGenerator
    {
        private static readonly char[] Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        public static string GenerateToken(int length)
        {
            var rng = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => Chars[rng.Next(Chars.Length)]).ToArray());
        }
    }
}
