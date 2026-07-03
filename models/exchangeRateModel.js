const dotenv = require('dotenv');
dotenv.config();

const cache = new Map();
const TTL_SECONDS = Number(process.env.RATE_CACHE_TTL_SECONDS || 300);

const fallbackRates = {
  USD: { INR: 85, EUR: 0.92, GBP: 0.79, AED: 3.67, CAD: 1.36, AUD: 1.52, SGD: 1.34, JPY: 156.2 },
  INR: { USD: 0.0118, EUR: 0.0108, GBP: 0.0093, AED: 0.0432, CAD: 0.016, AUD: 0.0179, SGD: 0.0158, JPY: 1.84 }
};

const fetchLiveRates = async (baseCurrency) => {
  const apiKey = process.env.EXCHANGE_RATE_API_KEY;
  const apiUrl = process.env.EXCHANGE_RATE_API_URL;
  if (!apiKey || !apiUrl) return null;
  try {
    const url = `${apiUrl}?base=${encodeURIComponent(baseCurrency)}&apikey=${encodeURIComponent(apiKey)}`;
    const response = await fetch(url, { headers: { Accept: 'application/json' } });
    if (!response.ok) return null;
    const data = await response.json();
    const rates = data.rates || data.conversion_rates || null;
    if (!rates) return null;
    return { rates, updatedAt: data.time_last_update_utc || new Date().toISOString() };
  } catch (error) {
    return null;
  }
};

const getExchangeRates = async (baseCurrency = 'USD') => {
  const cacheKey = baseCurrency;
  const cached = cache.get(cacheKey);
  const now = Date.now();
  if (cached && now - cached.storedAt < TTL_SECONDS * 1000) {
    return { baseCurrency, rates: cached.rates, cached: true, cacheTtlSeconds: TTL_SECONDS, updatedAt: cached.updatedAt };
  }
  const live = await fetchLiveRates(baseCurrency);
  const rates = live?.rates || fallbackRates[baseCurrency] || fallbackRates.USD;
  const updatedAt = live?.updatedAt || new Date().toISOString();
  cache.set(cacheKey, { rates, updatedAt, storedAt: now });
  return { baseCurrency, rates, cached: false, cacheTtlSeconds: TTL_SECONDS, updatedAt };
};

module.exports = { getExchangeRates };
