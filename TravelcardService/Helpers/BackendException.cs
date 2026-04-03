using System;

namespace TravelcardService.Helpers
{
    public class BackendException : Exception
    {
        public int StatusCode { get; }
        public string Details { get; }

        public BackendException(string message, string details, int statusCode) : base(message)
        {
            Details = details ?? string.Empty;
            StatusCode = statusCode;
        }
    }
}
