namespace AgeApi.Services
{
    public interface IAuthService
    {
        bool ValidateApiKey(string? apiKey);
    }
}
