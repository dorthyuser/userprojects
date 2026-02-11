using Project.SalesforceController.Models;

namespace Project.SalesforceController.Services;

public class InMemorySalesforceService : ISalesforceService
{
 private readonly List<AccountDto> _store = new();

 public InMemorySalesforceService()
 {
 // seed with some sample data
 for (int i = 1; i <= 50; i++)
 {
 _store.Add(new AccountDto
 {
 Id = i.ToString(),
 Name = $"Account {i}",
 Phone = $"+1-555-000{i:D3}",
 Website = $"https://account{i}.example.com"
 });
 }
 }

 public PagedResult<AccountDto> GetAccounts(int page, int pageSize)
 {
 var total = _store.Count;
 var items = _store.Skip((page - 1) * pageSize).Take(pageSize).ToList();
 return new PagedResult<AccountDto>
 {
 Page = page,
 PageSize = pageSize,
 TotalCount = total,
 Items = items
 };
 }

 public AccountDto? GetById(string id)
 {
 return _store.FirstOrDefault(x => x.Id == id);
 }

 public AccountDto Create(AccountDto dto)
 {
 if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();
 _store.Add(dto);
 return dto;
 }

 public AccountDto? Update(string id, AccountDto dto)
 {
 var existing = _store.FirstOrDefault(x => x.Id == id);
 if (existing == null) return null;
 existing.Name = dto.Name;
 existing.Phone = dto.Phone;
 existing.Website = dto.Website;
 return existing;
 }

 public bool Delete(string id)
 {
 var existing = _store.FirstOrDefault(x => x.Id == id);
 if (existing == null) return false;
 _store.Remove(existing);
 return true;
 }
}
