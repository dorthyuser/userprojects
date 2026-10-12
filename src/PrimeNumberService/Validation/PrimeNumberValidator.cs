namespace PrimeNumberService.Validation;

public sealed class PrimeNumberValidator
{
    private static readonly HashSet<int> Primes = new()
    {
        101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199
    };

    public int Validate(int value)
    {
        if (value < 100 || value > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "prime_number must be between 100 and 200");
        }

        if (!Primes.Contains(value))
        {
            throw new ArgumentException("prime_number must be prime", nameof(value));
        }

        return value;
    }
}