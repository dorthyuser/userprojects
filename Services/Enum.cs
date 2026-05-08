namespace Travelcardlambdacsharp847Lambda.Services;

public enum TravelcardTypeEnum
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

public enum CardholderTypeEnum
{
    Primary,
    Secondary
}

public static class EnumHelper
{
    public static string[] GetValues<T>() where T : struct, Enum => Enum.GetNames(typeof(T));
}