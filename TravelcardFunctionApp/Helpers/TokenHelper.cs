using System;
using System.Linq;

namespace TravelcardFunctionApp.Helpers
{
    public static class TokenHelper
    {
        private static readonly Random _rng = new Random();
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GenerateToken(int length)
        {
            return new string(Enumerable.Range(0, length).Select(_ => _chars[_rng.Next(_chars.Length)]).ToArray());
        }
    }
}
