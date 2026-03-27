using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace InitiateSwiftPayment.Helpers
{
    public static class Validators
    {
        public static bool IsValidIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;
            iban = iban.Replace(" ", string.Empty).ToUpperInvariant();
            if (iban.Length < 15 || iban.Length > 34) return false;

            // Move first four chars to the end
            var rearranged = iban.Substring(4) + iban.Substring(0, 4);

            // Convert letters to numbers (A=10, B=11, ...)
            var transformed = string.Concat(rearranged.Select(c => char.IsLetter(c) ? (c - 'A' + 10).ToString(CultureInfo.InvariantCulture) : c.ToString()));

            // Perform mod-97
            const int chunkSize = 6; // process in chunks
            var total = 0;
            var idx = 0;
            while (idx < transformed.Length)
            {
                var length = Math.Min(chunkSize, transformed.Length - idx);
                var part = total.ToString() + transformed.Substring(idx, length);
                total = (int)(BigIntegerModulo97(part));
                idx += length;
            }

            return total % 97 == 1;
        }

        private static long BigIntegerModulo97(string input)
        {
            // compute modulo 97 iteratively
            int pos = 0;
            long value = 0;
            while (pos < input.Length)
            {
                int len = Math.Min(9, input.Length - pos);
                var part = input.Substring(pos, len);
                value = (value * (long)Math.Pow(10, part.Length) + long.Parse(part)) % 97;
                pos += len;
            }

            return value;
        }

        public static bool IsValidBic(string bic)
        {
            if (string.IsNullOrWhiteSpace(bic)) return false;
            return Regex.IsMatch(bic, "^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$");
        }

        public static bool IsValidCurrency(string cur)
        {
            if (string.IsNullOrWhiteSpace(cur)) return false;
            return Regex.IsMatch(cur, "^[A-Z]{3}$");
        }
    }
}
