namespace hello.Application;

public sealed record ErrorResponse(string Detail, string? Code = null);
