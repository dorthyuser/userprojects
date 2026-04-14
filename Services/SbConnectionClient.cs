using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace tc_csharp_api
{
    /// <summary>
    /// sb-connection module: configures an HttpClient that ensures OAuth2 tokens are attached to outgoing requests.
    /// All configuration and secret resolution must come from IConfiguration or environment variables.
    /// </summary>
    public static class SbConnectionClient
    {
        /// <summary>
        /// Registers the sb-connection HttpClient along with the OAuthTokenHandler and required services.
        /// Usage: services.AddSbConnection(builder.Configuration);
        /// </summary>
        public static IServiceCollection AddSbConnection(this IServiceCollection services)
        {
            // Register the delegating handler and named client. TokenService and token-client must be registered by the caller.
            services.AddTransient<OAuthTokenHandler>();

            services.AddHttpClient("sb-connection")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
                .AddHttpMessageHandler<OAuthTokenHandler>();

            return services;
        }
    }
}
