namespace travelcard_function_app.Helpers;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static ValidationResult Ok()
    {
        return new ValidationResult { IsValid = true };
    }

    public static ValidationResult Fail(string message, string code)
    {
        return new ValidationResult { IsValid = false, ErrorMessage = message, ErrorCode = code };
    }
}
