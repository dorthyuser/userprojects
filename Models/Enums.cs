using System.Text.Json.Serialization;

namespace AdverseEventReporter.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Outcome
{
    ONGOING,
    RESOLVED,
    FATAL,
    UNKNOWN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionTaken
{
    NONE,
    DOSE_REDUCED,
    DRUG_WITHDRAWN,
    HOSPITALISED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    HIGH,
    NORMAL
}
