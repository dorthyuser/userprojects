using System.Threading.Tasks;

namespace InitiateSwiftPayment.Helpers
{
    public class CbsClient
    {
        // Mocked CBS integration for sufficient funds check
        public Task<bool> HasSufficientFundsAsync(string debtorIban, decimal amount, string currency)
        {
            // For demo: allow if amount <= 5000000.00
            var allowed = amount <= 5000000M;
            return Task.FromResult(allowed);
        }
    }
}
