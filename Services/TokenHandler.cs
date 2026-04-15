using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Services
{
    /// <summary>
    /// DelegatingHandler that ensures an OAuth2 bearer token is attached to outgoing requests.
    /// It will attempt to refresh the token on HTTP 401 responses and retry once.
    /// </summary>
    public class TokenHandler : DelegatingHandler
    {
        private readonly ZohoAuthService _authService;
        private readonly ILogger<TokenHandler> _logger;

        public TokenHandler(ZohoAuthService authService, ILogger<TokenHandler> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to attach access token to request.");
                throw;
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // try refresh and retry once
                _logger.LogInformation("Received 401 from upstream. Attempting token refresh and retry.");
                try
                {
                    var refreshed = await _authService.RefreshTokenAsync();
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

                    // dispose previous response before retry
                    response.Dispose();
                    response = await base.SendAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token refresh attempt failed.");
                    // rethrow original 401 downstream
                }
            }

            return response;
        }
    }
}
