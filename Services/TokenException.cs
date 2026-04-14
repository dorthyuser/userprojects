using System;

namespace tc_csharp_api
{
    public class TokenException : Exception
    {
        public TokenException(string message) : base(message) { }
    }
}
