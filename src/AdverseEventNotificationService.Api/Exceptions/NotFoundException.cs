namespace AdverseEventNotificationService.Api.Exceptions;

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string code) : base(code, code, StatusCodes.Status404NotFound)
    {
    }
}