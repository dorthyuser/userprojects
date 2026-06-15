namespace travelcard_function_app.Models;

public class ErrorResponse
{
    public ErrorDetail Error { get; set; } = new();
}
