using System;

namespace InitiateSwiftPayment.Helpers
{
    public static class IbanValidator
    {
        public static bool Validate(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;
            var input = iban.Replace(" ", string.Empty).ToUpperInvariant();
            if (input.Length < 15 || input.Length > 34) return false;

            // Move first four chars to end
            var rearranged = input.Substring(4) + input.Substring(0, 4);

            // Replace letters with numbers A=10 ... Z=35
            var sb = new System.Text.StringBuilder();
            foreach (var c in rearranged)
            {
                if (char.IsLetter(c))
                {
                    sb.Append(((int)c - 55).ToString());
                }
                else if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    return false;
                }
            }

            var numeric = sb.ToString();

            // Perform mod-97 using chunks to avoid big integers
            var remainder = 0;
            var pos = 0;
            while (pos < numeric.Length)
            {
                var block = remainder.ToString() + numeric.Substring(pos, Math.Min(9, numeric.Length - pos));
                remainder = int.Parse(block) % 97;
                pos += Math.Min(9, numeric.Length - pos);
            }

            return remainder == 1;
        }
    }
}
