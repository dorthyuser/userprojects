using System.ComponentModel.DataAnnotations;

namespace hello.Application;

public sealed class PrimeResponse
{
    [Required]
    [Range(100, 200)]
    public int Prime { get; init; }
}
