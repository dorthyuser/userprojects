using System;
using System.Net;

namespace AdverseEventReporter.Helpers;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Code { get; }

    public ApiException(HttpStatusCode statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}
