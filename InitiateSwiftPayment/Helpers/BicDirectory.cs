using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace InitiateSwiftPayment.Helpers
{
    public class BicDirectory
    {
        private readonly HashSet<string> _bics = new HashSet<string>
        {
            "DEUTDEFFXXX",
            "DEUTDEFF",
            "BOFAUS3N",
            "BANKUS33",
            "NEDSZAJJ"
        };

        private readonly Regex _bicRegex = new Regex("^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$", RegexOptions.Compiled);

        public bool IsValidFormat(string bic)
        {
            if (string.IsNullOrWhiteSpace(bic)) return false;
            return _bicRegex.IsMatch(bic);
        }

        public bool Exists(string bic)
        {
            if (string.IsNullOrWhiteSpace(bic)) return false;
            // Normalize
            var key = bic.Trim().ToUpperInvariant();
            return _bics.Contains(key);
        }

        public bool IsCurrencySupported(string debtorBic, string creditorBic, string currency)
        {
            // Simplified corridor check: for demo, allow all currencies for known BICs, else false
            if (!Exists(debtorBic) || !Exists(creditorBic)) return false;
            return true;
        }
    }
}
