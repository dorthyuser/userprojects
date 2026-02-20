using System;
using System.Security.Cryptography;

namespace CreateTravelcard.Helpers
{
    public class TokenGenerator
    {
        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
