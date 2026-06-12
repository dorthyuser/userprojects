namespace TravelCardFunctionApp.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true, ErrorMessage = string.Empty };
    }

    public static ValidationResult Fail(string message)
    {
        return new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}
