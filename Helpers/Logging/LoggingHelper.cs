using Microsoft.Extensions.Logging;

namespace TravelCardFunctionApp.Helpers
{
    public static class LoggingHelper
    {
        public static void LogException(ILogger logger, System.Exception ex, string traceId)
        {
            logger.LogError(ex, "TraceId:{TraceId} Message:{Message}", traceId, ex.Message);
        }
    }
}
