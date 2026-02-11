using Project.SalesforceController.Models;

namespace Project.SalesforceController.Services;

public interface ISalesforceService
{
 PagedResult<AccountDto> GetAccounts(int page, int pageSize);
 AccountDto? GetById(string id);
 AccountDto Create(AccountDto dto);
 AccountDto? Update(string id, AccountDto dto);
 bool Delete(string id);
}
