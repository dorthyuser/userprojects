namespace AdverseEventNotificationService.Api.Exceptions;

public sealed class ConflictException : ApiException
{
    public ConflictException(string code) : base(code, code, StatusCodes.Status409Conflict)
    {
    }
}