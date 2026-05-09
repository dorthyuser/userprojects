using System.ComponentModel.DataAnnotations;

namespace testing2.Models;

public sealed class TestRequest
{
    [Required]
    public string Input { get; set; } = string.Empty;
}