const currencies = [
  { code: 'USD', symbol: '$', name: 'US Dollar' },
  { code: 'INR', symbol: '₹', name: 'Indian Rupee' },
  { code: 'EUR', symbol: '€', name: 'Euro' },
  { code: 'GBP', symbol: '£', name: 'British Pound' },
  { code: 'AED', symbol: 'د.إ', name: 'UAE Dirham' },
  { code: 'CAD', symbol: 'C$', name: 'Canadian Dollar' },
  { code: 'AUD', symbol: 'A$', name: 'Australian Dollar' },
  { code: 'SGD', symbol: 'S$', name: 'Singapore Dollar' },
  { code: 'JPY', symbol: '¥', name: 'Japanese Yen' }
];

const getSupportedCurrencies = () => currencies;
const getCurrencyMeta = (code) => currencies.find((c) => c.code === code) || { code, symbol: '', name: code };
const isValidCurrencyCode = (code) => currencies.some((c) => c.code === code);

module.exports = { getSupportedCurrencies, getCurrencyMeta, isValidCurrencyCode };
