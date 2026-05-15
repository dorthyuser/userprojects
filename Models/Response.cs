namespace Travelcarddemo1251Lambda.Models;

public class TravelcardResponse
{
    public string TravelcardId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}