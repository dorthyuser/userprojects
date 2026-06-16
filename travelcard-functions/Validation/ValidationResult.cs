namespace travelcard_functions.Validation;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static ValidationResult Ok() => new ValidationResult { IsValid = true };
    public static ValidationResult Fail(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
}
