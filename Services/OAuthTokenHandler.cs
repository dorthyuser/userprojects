using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace tc_csharp_api
{
    /// <summary>
    /// DelegatingHandler that attaches a Bearer token to outgoing requests and refreshes on 401 responses.
    /// Token acquisition is delegated to ITokenService.
    /// </summary>
    public class OAuthTokenHandler : DelegatingHandler
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<OAuthTokenHandler> _logger;

        public OAuthTokenHandler(ITokenService tokenService, ILogger<OAuthTokenHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await _tokenService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await base.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("Received 401 - refreshing token and retrying once");
                    await _tokenService.ForceRefreshAsync();
                    var newToken = await _tokenService.GetTokenAsync();
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
                    }

                    response = await base.SendAsync(request, cancellationToken);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OAuthTokenHandler");
                throw;
            }
        }
    }
}
