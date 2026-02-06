namespace AgeApi.Models
{
    public class AgeResponse
    {
        public long Days { get; set; }
        public long Weeks { get; set; }
        public long Minutes { get; set; }
        public long Seconds { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}
