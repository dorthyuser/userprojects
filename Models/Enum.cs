namespace Travelcardlambdacsharp1133Lambda.Models;

public enum TravelcardType
{
    Young,
    Barcklays,
    DevonandCornwall,
    TwoTogether,
    Family,
    Senior,
    DisabledPersons,
    Network,
    TwentySixToThirty,
    SixteenToSeventeen,
    Veterans
}

public enum CardholderType
{
    Primary,
    Secondary
}

public static class EnumHelper
{
    public static string[] GetNames<T>() where T : struct, Enum => Enum.GetNames(typeof(T));
}
