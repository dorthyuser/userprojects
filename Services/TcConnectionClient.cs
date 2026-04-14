using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace tc_testing_api2.Services
{
    /// <summary>
    /// Provides a configured HTTP client for the tc-connection module.
    /// Uses IHttpClientFactory to create the named client and sends requests with provided headers.
    /// </summary>
    public class TcConnectionClient
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TcConnectionClient> _logger;

        /// <summary>
        /// Constructor that receives IHttpClientFactory and IConfiguration via DI.
        /// </summary>
        public TcConnectionClient(IHttpClientFactory factory, IConfiguration configuration, ILogger<TcConnectionClient> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Post content to the specified URL using the configured tc-connection HttpClient.
        /// </summary>
        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, IDictionary<string, string>? headers = null)
        {
            try
            {
                var client = _factory.CreateClient("tc-connection");

                // Add headers for this request without mutating shared default headers
                using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

                if (headers != null)
                {
                    foreach (var kv in headers)
                    {
                        if (!req.Headers.Contains(kv.Key))
                            req.Headers.Add(kv.Key, kv.Value);
                    }
                }

                // Ensure Content-Type header is present on the content
                if (req.Content != null && !req.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                }

                var response = await client.SendAsync(req);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request via TcConnectionClient");
                throw;
            }
        }
    }
}