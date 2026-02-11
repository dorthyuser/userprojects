using Project.SalesforceApi.Models;

namespace Project.SalesforceApi.Services
{
 public interface ISalesforceService
 {
 PagedResult<AccountDto> GetAccounts(int page, int pageSize);
 AccountDto? GetById(string id);
 AccountDto Create(AccountDto dto);
 bool Update(string id, AccountDto dto);
 bool Delete(string id);
 }
}
