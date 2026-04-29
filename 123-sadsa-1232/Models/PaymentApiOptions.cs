namespace _123_sadsa_1232.Models;

public sealed class PaymentApiOptions
{
    public string Provider { get; set; } = "PostgreSql";
    public string HttpBaseUrl { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;
    public string IciciEndpointPath { get; set; } = "/icici/payments";
}