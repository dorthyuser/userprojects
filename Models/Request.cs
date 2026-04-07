using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TcTesting9Lambda.Models;

namespace TcTesting9Lambda.Models
{
    public class Request
    {
        // The raw payload as received from the client
        [JsonPropertyName("payload")]
        public JsonElement Payload { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("query")]
        public Dictionary<string, string>? QueryStringParameters { get; set; }

        [JsonPropertyName("path")]
        public Dictionary<string, string>? PathParameters { get; set; }

        public Request()
        {
            Headers = new Dictionary<string, string>();
            QueryStringParameters = new Dictionary<string, string>();
            PathParameters = new Dictionary<string, string>();
        }
    }
}
