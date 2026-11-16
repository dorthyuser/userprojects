using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BusTravelAccountsSaFunction;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SalesforceOptions>()
            .Bind(configuration.GetSection(SalesforceOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AwsOptions>()
            .Bind(configuration.GetSection(AwsOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ISalesforceClient, SalesforceClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<SalesforceOptions>>().Value;
                client.Timeout = TimeSpan.FromMilliseconds(options.ResponseTimeoutMilliseconds);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<SalesforceOptions>>().Value;
                return new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromMilliseconds(options.ConnectTimeoutMilliseconds)
                };
            });

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
            return new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
                RegionEndpoint.GetBySystemName(options.Region));
        });

        services.AddScoped<IAccountsRepository, AccountsRepository>();
        services.AddScoped<IErrorAuditClient, ErrorAuditClient>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<IHealthService, HealthService>();

        return services;
    }
}
