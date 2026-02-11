namespace Project.SalesforceController.Models;

public class AccountDto
{
 public string Id { get; set; } = Guid.NewGuid().ToString();
 public string? Name { get; set; }
 public string? Phone { get; set; }
 public string? Website { get; set; }
}
