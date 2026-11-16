namespace BusTravelAccountsSaFunction;

public interface IErrorAuditClient
{
    Task WriteErrorAsync(ErrorResponse error, string httpMethod, string endpoint, CancellationToken cancellationToken);
}
