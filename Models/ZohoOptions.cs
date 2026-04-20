using System;

namespace ZohoProject3.Models
{
    public class ZohoOptions
    {
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
        public string TokenUrl { get; set; } = null!;
        public string Scopes { get; set; } = "ZohoCRM.users.ALL";
    }
}
