namespace Csharpae1039Lambda;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AeOutcome
{
    ONGOING,
    RESOLVED,
    FATAL,
    UNKNOWN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AeActionTaken
{
    NONE,
    DOSE_REDUCED,
    DRUG_WITHDRAWN,
    HOSPITALISED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationPriority
{
    HIGH,
    NORMAL
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuditAction
{
    CREATED
}