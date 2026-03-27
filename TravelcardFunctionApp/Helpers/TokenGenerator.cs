using System;
using System.Security.Cryptography;
using System.Text;

namespace TravelcardFunctionApp.Helpers
{
    public class TokenGenerator
    {
        public string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var data = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);
            var sb = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                sb.Append(chars[data[i] % chars.Length]);
            }

            return sb.ToString();
        }
    }
}
