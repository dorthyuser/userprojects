using System;

namespace tc_testing_api2.Services
{
    /// <summary>
    /// Exception thrown when token acquisition or refresh fails.
    /// </summary>
    public class TokenException : Exception
    {
        public TokenException() { }
        public TokenException(string message) : base(message) { }
        public TokenException(string message, Exception inner) : base(message, inner) { }
    }
}