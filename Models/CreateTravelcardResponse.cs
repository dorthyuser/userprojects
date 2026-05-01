namespace azuretravelcardfunction309.Models;

public sealed class CreateTravelcardResponse
{
    public string travelcardId { get; set; }
    public string token { get; set; }

    public CreateTravelcardResponse(string travelcardId, string token)
    {
        this.travelcardId = travelcardId;
        this.token = token;
    }
}
