using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ResponseHttp.Services
{
    /// <summary>
    /// Provides extension method to register a configured HTTP client (named "http") with Basic Authentication.
    /// Reads credentials from configuration keys "$testuser" and "$testpass" or environment variables with the same names.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Registers and configures the named HTTP client "http" using IConfiguration values for provider and port and a message handler that applies Basic Auth.
        /// </summary>
        public static IServiceCollection AddHttpConnection(this IServiceCollection services, IConfiguration configuration)
        {
            // Register the delegating handler which will add Basic Auth header to every outgoing request.
            services.AddTransient<BasicAuthHandler>();

            services.AddHttpClient("http", (provider, client) =>
            {
                // Optionally configure base address from configuration values Http:Provider and Http:Port.
                var config = provider.GetRequiredService<IConfiguration>();

                var providerUrl = config["Http:Provider"];
                var port = config["Http:Port"];

                if (!string.IsNullOrEmpty(providerUrl) && !string.IsNullOrEmpty(port))
                {
                    // Attempt to create a Uri from provider and port. Do not hardcode any value here.
                    try
                    {
                        var trimmed = providerUrl.TrimEnd('/');
                        var combined = trimmed.Contains(":") ? $"{trimmed}:{port}" : $"{trimmed}:{port}";

                        if (Uri.TryCreate(combined, UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                    }
                    catch (Exception ex)
                    {
                        var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("HttpClientExtensions");
                        logger?.LogError(ex, "Failed to configure HttpClient base address from configuration.");
                        throw;
                    }
                }

                // Additional client defaults may be applied here.
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<BasicAuthHandler>();

            return services;
        }
    }

    /// <summary>
    /// DelegatingHandler that reads username and password from configuration keys "$testuser" and "$testpass" or environment variables and applies Basic Authentication header to each request.
    /// </summary>
    public class BasicAuthHandler : DelegatingHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BasicAuthHandler> _logger;

        /// <summary>
        /// Constructs the handler with injected IConfiguration and ILogger.
        /// </summary>
        public BasicAuthHandler(IConfiguration configuration, ILogger<BasicAuthHandler> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Intercepts outgoing requests to append the Authorization header using Basic auth with credentials read from configuration or environment.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Resolve credentials using the exact keys required.
            var username = _configuration["$testuser"] ?? Environment.GetEnvironmentVariable("$testuser");
            var password = _configuration["$testpass"] ?? Environment.GetEnvironmentVariable("$testpass");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogError("Basic authentication configured but credentials are missing from configuration or environment variables ($testuser/$testpass).");
                throw new InvalidOperationException("Missing basic authentication credentials.");
            }

            try
            {
                var credentialBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
                var base64 = Convert.ToBase64String(credentialBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Basic Authorization header.");
                throw;
            }

            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed in BasicAuthHandler.");
                throw;
            }
        }
    }
}
