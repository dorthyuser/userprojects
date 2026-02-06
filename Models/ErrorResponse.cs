using System.Collections.Generic;

namespace AgeApi.Models
{
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public IDictionary<string, string?> Details { get; set; } = new Dictionary<string, string?>();
    }
}
