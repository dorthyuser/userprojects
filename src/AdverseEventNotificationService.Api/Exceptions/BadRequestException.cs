namespace AdverseEventNotificationService.Api.Exceptions;

public sealed class BadRequestException : ApiException
{
    public BadRequestException(string code) : base(code, code, StatusCodes.Status400BadRequest)
    {
    }
}