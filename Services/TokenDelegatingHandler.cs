using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Services
{
    /// <summary>
    /// DelegatingHandler that attaches Bearer token and attempts a single retry
    /// on 401 responses by forcing a token refresh.
    /// </summary>
    public class TokenDelegatingHandler : DelegatingHandler
    {
        private readonly TokenService _tokenService;
        private readonly ILogger<TokenDelegatingHandler> _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TokenDelegatingHandler(TokenService tokenService, ILogger<TokenDelegatingHandler> logger)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await _tokenService.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await base.SendAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Received 401 from upstream, attempting token refresh and retry.");
                    await _tokenService.ForceRefreshAsync();
                    var newToken = await _tokenService.GetAccessTokenAsync();
                    if (!string.IsNullOrWhiteSpace(newToken))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
                    }

                    // Dispose previous response then retry once
                    response.Dispose();
                    response = await base.SendAsync(request, cancellationToken);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TokenDelegatingHandler.");
                throw;
            }
        }
    }
}
