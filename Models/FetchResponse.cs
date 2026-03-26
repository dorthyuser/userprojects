namespace ResponseHttp.Models
{
    /// <summary>
    /// Simple model representing the formatted response payload returned by the API.
    /// </summary>
    public class FetchResponse
    {
        /// <summary>
        /// The concatenated response string: "Response received- <<response>>."
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }
}
