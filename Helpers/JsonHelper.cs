using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdverseEventReporter.Helpers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> DeserializeAsync<T>(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, Options);
    }
}
