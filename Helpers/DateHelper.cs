using System;

namespace AgeApi.Helpers
{
    public static class DateHelper
    {
        public static bool TryParseDob(string? input, out DateTime dobUtc)
        {
            dobUtc = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Try parse using several common formats
            var formats = new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ssZ", "o" };

            if (DateTimeOffset.TryParse(input, out var dto))
            {
                dobUtc = dto.UtcDateTime;
                return true;
            }

            foreach (var fmt in formats)
            {
                if (DateTime.TryParseExact(input, fmt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                {
                    dobUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    return true;
                }
            }

            // Last attempt: general parse
            if (DateTime.TryParse(input, out var genDate))
            {
                dobUtc = DateTime.SpecifyKind(genDate, DateTimeKind.Utc);
                return true;
            }

            return false;
        }

        public static (long days, long weeks, long minutes, long seconds) CalculateAgeComponents(DateTime dobUtc, DateTime nowUtc)
        {
            var span = nowUtc - dobUtc;
            if (span.Ticks < 0) span = TimeSpan.Zero;

            var days = (long)Math.Floor(span.TotalDays);
            var weeks = days / 7;
            var minutes = (long)Math.Floor(span.TotalMinutes);
            var seconds = (long)Math.Floor(span.TotalSeconds);
            return (days, weeks, minutes, seconds);
        }
    }
}
