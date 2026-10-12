namespace PrimeNumberService.Security;

public sealed record DatabaseSecret(string Host, int Port, string DbName, string Username, string Password);