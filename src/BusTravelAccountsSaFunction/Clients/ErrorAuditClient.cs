using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BusTravelAccountsSaFunction;

public sealed class ErrorAuditClient(IAmazonS3 s3, IOptions<AwsOptions> options, ILogger<ErrorAuditClient> logger) : IErrorAuditClient
{
    public async Task WriteErrorAsync(ErrorResponse error, string httpMethod, string endpoint, CancellationToken cancellationToken)
    {
        var description = error.Error.ErrorDescription;
        if (description.Length > 256)
        {
            description = description[..256];
        }

        var payload = new
        {
            errorCode = error.Error.ErrorCode,
            httpMethod,
            httpErrorCode = error.Error.ErrorCode,
            errorMessage = error.Error.ErrorMessage,
            errorDescription = description,
            timestamp = error.Error.ErrorDateTime,
            apiName = "bus-travel-accounts-sa_main",
            endpoint
        };

        var key = $"bus-travel-accounts-sa_main-{DateTimeOffset.UtcNow:O}";
        var body = JsonSerializer.Serialize(payload, JsonDefaults.Options);

        logger.LogError("S3 put object: {Bucket} {Key}", options.Value.Bucket, key);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.Bucket,
            Key = key,
            ContentBody = body
        }, cancellationToken);
        logger.LogInformation("S3 put object completed: {Bucket} {Key}", options.Value.Bucket, key);
    }
}
