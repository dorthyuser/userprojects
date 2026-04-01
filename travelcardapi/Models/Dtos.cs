using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardApi.Models
{
    public class Connection
    {
        public string Name { get; set; } = string.Empty;
        public Protocol Protocol { get; set; } = Protocol.HTTPS;
        public AuthMethod AuthMethod { get; set; } = AuthMethod.oauth2;
    }

    public class OAuth2Settings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public string? AuthorizationUrl { get; set; } = null;
        public List<string> Scopes { get; set; } = new List<string>();

        public void ResolveEnvironmentPlaceholders()
        {
            if (!string.IsNullOrEmpty(ClientId) && ClientId.StartsWith("$"))
            {
                var env = Environment.GetEnvironmentVariable(ClientId.TrimStart('$')) ?? string.Empty;
                ClientId = env;
            }

            if (!string.IsNullOrEmpty(ClientSecret) && ClientSecret.StartsWith("$"))
            {
                var env = Environment.GetEnvironmentVariable(ClientSecret.TrimStart('$')) ?? string.Empty;
                ClientSecret = env;
            }

            if (!string.IsNullOrEmpty(TokenUrl) && TokenUrl.StartsWith("$"))
            {
                var env = Environment.GetEnvironmentVariable(TokenUrl.TrimStart('$')) ?? string.Empty;
                TokenUrl = env;
            }

            for (int i = 0; i < Scopes.Count; i++)
            {
                var s = Scopes[i];
                if (!string.IsNullOrEmpty(s) && s.StartsWith("$"))
                {
                    Scopes[i] = Environment.GetEnvironmentVariable(s.TrimStart('$')) ?? string.Empty;
                }
            }
        }
    }

    public class Auth
    {
        [JsonPropertyName("oauth2")]
        public OAuth2Settings OAuth2 { get; set; } = new OAuth2Settings();
    }

    public class TravelcardRequest
    {
        public Connection Connection { get; set; } = new Connection();
        public Auth Auth { get; set; } = new Auth();
    }
}
