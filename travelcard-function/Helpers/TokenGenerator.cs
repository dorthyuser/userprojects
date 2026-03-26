using System;
using System.Linq;

namespace TravelcardFunction.Helpers
{
    public static class TokenGenerator
    {
        private static readonly Random _rnd = new Random();
        public static string Generate(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Range(0, length).Select(_ => chars[_rnd.Next(chars.Length)]).ToArray());
        }
    }
}
