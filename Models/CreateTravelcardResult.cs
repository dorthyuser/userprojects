namespace TravelcardFunctionApp.Models;

public sealed class CreateTravelcardResult
{
    public string TravelcardId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}