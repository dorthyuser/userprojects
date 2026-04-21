namespace zoho_project_csharp.Models
{
    public class ZohoOptions
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TokenUrl { get; set; }
        public string? RefreshToken { get; set; }
        public string? BaseUrl { get; set; }
        public string? Scopes { get; set; }
        public int Port { get; set; }
        public string? Provider { get; set; }
    }
}
