namespace BusTravelAccountsSaMainLambda;

public sealed class BasicDetails
{
    public string? CustomerCorrelationId { get; set; }
    public string? XCorrelationId { get; set; }
    public string? ClientId { get; set; }
    public string? HttpMethod { get; set; }
    public string? RelativePath { get; set; }
}