using System.Text.Json.Serialization;

namespace Csharpae1140Lambda;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdverseEventOutcomeEnum
{
    ONGOING,
    RESOLVED,
    FATAL,
    UNKNOWN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdverseEventActionTakenEnum
{
    NONE,
    DOSE_REDUCED,
    DRUG_WITHDRAWN,
    HOSPITALISED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationPriorityEnum
{
    HIGH,
    NORMAL
}