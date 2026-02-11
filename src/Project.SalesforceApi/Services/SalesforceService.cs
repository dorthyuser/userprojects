using Project.SalesforceApi.Models;

namespace Project.SalesforceApi.Services
{
 // Minimal placeholder in-memory implementation so app can run. Replace with real Salesforce integration.
 public class SalesforceService : ISalesforceService
 {
 private readonly List<AccountDto> _store = new();

 public SalesforceService()
 {
 // Seed some sample data
 for (int i = 1; i <= 50; i++)
 {
 _store.Add(new AccountDto
 {
 Id = i.ToString(),
 Name = $"Account {i}",
 Phone = $"555-00{i:00}",
 Industry = i % 2 == 0 ? "Technology" : "Finance"
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
 return _store.FirstOrDefault(a => a.Id == id);
 }

 public AccountDto Create(AccountDto dto)
 {
 var id = (_store.Count + 1).ToString();
 dto.Id = id;
 _store.Add(dto);
 return dto;
 }

 public bool Update(string id, AccountDto dto)
 {
 var existing = _store.FirstOrDefault(a => a.Id == id);
 if (existing == null) return false;
 existing.Name = dto.Name;
 existing.Phone = dto.Phone;
 existing.Industry = dto.Industry;
 return true;
 }

 public bool Delete(string id)
 {
 var existing = _store.FirstOrDefault(a => a.Id == id);
 if (existing == null) return false;
 _store.Remove(existing);
 return true;
 }
 }
}
