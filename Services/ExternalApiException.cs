using System;
using System.Net;

namespace tc_csharp_api
{
    public class ExternalApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ExternalApiException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
