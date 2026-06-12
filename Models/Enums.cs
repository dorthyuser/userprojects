using System.Text.Json.Serialization;

namespace azurefunctionaeproject.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutcomeEnum
{
    ONGOING,
    RESOLVED,
    FATAL,
    UNKNOWN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionTakenEnum
{
    NONE,
    DOSE_REDUCED,
    DRUG_WITHDRAWN,
    HOSPITALISED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PriorityEnum
{
    HIGH,
    NORMAL
}
