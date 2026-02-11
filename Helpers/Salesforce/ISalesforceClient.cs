using System.Threading.Tasks;
using SalesforceAccountFunctions.Models;
namespace SalesforceAccountFunctions.Helpers.Salesforce
{
    public interface ISalesforceClient
    {
        Task<SalesforceCreateResult> CreateAccountAsync(AccountDto account);
        Task<AccountDto?> GetAccountAsync(string id);
        Task<SalesforceDeleteResult> DeleteAccountAsync(string id);
    }
}
