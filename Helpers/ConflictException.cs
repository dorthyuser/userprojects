using System;

namespace azurefunctionaeproject.Helpers;

public sealed class ConflictException : Exception
{
    public string Code { get; }
    public string? ExistingAeId { get; }

    public ConflictException(string code, string message, string? existingAeId = null) : base(message)
    {
        Code = code;
        ExistingAeId = existingAeId;
    }
}
