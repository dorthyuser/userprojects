using System;

namespace azurefunctionaeproject.Helpers;

public sealed class NotFoundException : Exception
{
    public string Code { get; }

    public NotFoundException(string code, string message) : base(message)
    {
        Code = code;
    }
}
