using System;

namespace azurefunctionaeproject.Helpers;

public sealed class ValidationException : Exception
{
    public string Code { get; }

    public ValidationException(string code, string message) : base(message)
    {
        Code = code;
    }
}
