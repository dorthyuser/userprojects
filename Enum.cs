namespace Csharpae1012Lambda;

using System.Text.Json.Serialization;

public enum Outcome
{
    ONGOING,
    RESOLVED,
    FATAL,
    UNKNOWN
}

public enum ActionTaken
{
    NONE,
    DOSE_REDUCED,
    DRUG_WITHDRAWN,
    HOSPITALISED
}

public enum Priority
{
    HIGH,
    NORMAL
}

public enum LogLevelName
{
    DEBUG,
    INFO,
    WARNING,
    ERROR
}

public static class EnumMetadata
{
    public static readonly JsonStringEnumConverter JsonEnumConverter = new(null, allowIntegerValues: false);
}