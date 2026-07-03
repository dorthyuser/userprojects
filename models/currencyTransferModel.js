const https = require('https');

const SUPPORTED_CURRENCIES = [
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

const CACHE_TTL_MS = 10 * 60 * 1000;
const rateCache = new Map();

function round2(value) {
  return Math.round((Number(value) + Number.EPSILON) * 100) / 100;
}

function getCurrencyMeta(code) {
  return SUPPORTED_CURRENCIES.find((item) => item.code === code) || null;
}

function validatePayload(payload) {
  const errors = [];
  const fromCurrency = String(payload.fromCurrency || '').toUpperCase();
  const toCurrency = String(payload.toCurrency || '').toUpperCase();
  const amount = Number(payload.amount);

  if (!fromCurrency || !getCurrencyMeta(fromCurrency)) errors.push({ field: 'fromCurrency', message: 'Unsupported or missing currency code' });
  if (!toCurrency || !getCurrencyMeta(toCurrency)) errors.push({ field: 'toCurrency', message: 'Unsupported or missing currency code' });
  if (!Number.isFinite(amount) || amount <= 0) errors.push({ field: 'amount', message: 'Amount must be a positive number' });
  if (fromCurrency && toCurrency && fromCurrency === toCurrency) errors.push({ field: 'toCurrency', message: 'fromCurrency and toCurrency must be different' });

  return { valid: errors.length === 0, errors, normalized: { fromCurrency, toCurrency, amount: Number.isFinite(amount) ? amount : null } };
}

function fetchJson(url) {
  return new Promise((resolve, reject) => {
    https.get(url, { headers: { Accept: 'application/json' } }, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        try {
          resolve(JSON.parse(data));
        } catch (error) {
          reject(error);
        }
      });
    }).on('error', reject);
  });
}

async function getLiveRates(baseCurrency) {
  const cached = rateCache.get(baseCurrency);
  if (cached && Date.now() - cached.timestamp < CACHE_TTL_MS) return { ...cached.data, cached: true };
  const provider = process.env.EXCHANGE_RATE_API_URL || `https://open.er-api.com/v6/latest/${encodeURIComponent(baseCurrency)}`;
  const data = await fetchJson(provider);
  const rates = data && data.rates ? data.rates : {};
  const result = { baseCurrency, rates, cached: false, timestamp: new Date().toISOString() };
  rateCache.set(baseCurrency, { timestamp: Date.now(), data: result });
  return result;
}

class CurrencyTransferService {
  async health() {
    try {
      return { status: 'ok', service: 'currency-transfer-calculator', dependencies: { exchangeRateProvider: 'ok', cache: 'ok' }, timestamp: new Date().toISOString() };
    } catch (error) {
      console.error(' Failed', error && error.message ? error.message : error);
      throw error;
    }
  }

  async getCurrencies() {
    try {
      return { supportedCurrencies: SUPPORTED_CURRENCIES };
    } catch (error) {
      console.error(' Failed', error && error.message ? error.message : error);
      throw error;
    }
  }

  async getRates(baseCurrency) {
    try {
      const normalizedBase = String(baseCurrency || 'USD').toUpperCase();
      if (!getCurrencyMeta(normalizedBase)) throw new Error('Unsupported base currency');
      const live = await getLiveRates(normalizedBase);
      const rates = {};
      for (const code of ['INR', 'EUR', 'GBP', 'AED', 'CAD', 'AUD', 'SGD', 'JPY']) {
        if (live.rates[code] != null) rates[code] = round2(live.rates[code]);
      }
      return { baseCurrency: normalizedBase, rates, cached: live.cached, timestamp: live.timestamp };
    } catch (error) {
      console.error(' Failed', error && error.message ? error.message : error);
      throw error;
    }
  }

  async getSwagger() {
    try {
      return { documentation: { title: 'Currency Transfer Calculator API', version: '1.0.0', format: 'OpenAPI 3.0' }, paths: ['/api/v1/currency-transfer/calculate', '/api/v1/currency-transfer/currencies', '/api/v1/currency-transfer/rates', '/api/v1/currency-transfer/health', '/api/v1/currency-transfer/validate'] };
    } catch (error) {
      console.error(' Failed', error && error.message ? error.message : error);
      throw error;
    }
  }

  async validateTransfer(payload) {
    try {
      return validatePayload(payload);
    } catch (error) {
      console.error(' Failed', error && error.message ? error.message : error);
      throw error;
    }
  }

  async calculateTransfer(payload) {
    try {
      const validation = validatePayload(payload);
      if (!validation.valid) {
        const err = new Error('Invalid transfer request');
        err.details = validation.errors;
        throw err;
      }
      const { fromCurrency, toCurrency, amount } = validation.normalized;
      const fromMeta = getCurrencyMeta(fromCurrency);
      const toMeta = getCurrencyMeta(toCurrency);
      const live = await getLiveRates(fromCurrency);
      const marketRate = Number(live.rates[toCurrency]);
      if (!Number.isFinite(marketRate)) throw new Error('Exchange rate unavailable');
      const bankRate = round2(marketRate * 0.99);
      const grossAmountInINR = round2(amount * marketRate);
      const transferFee = round2(Math.max(0.5, amount * 0.005));
      const platformFee = round2(Math.max(0.2, amount * 0.002));
      const gst = round2((transferFee + platformFee) * 0.18);
      const totalFees = round2(transferFee + platformFee + gst);
      const exchangeLoss = round2((marketRate - bankRate) * amount);
      const netAmountReceived = round2(grossAmountInINR - totalFees - exchangeLoss);
      return { source: { currency: fromCurrency, symbol: fromMeta.symbol, amount: round2(amount) }, destination: { currency: toCurrency, symbol: toMeta.symbol }, marketRate: round2(marketRate), bankRate: round2(bankRate), grossAmountInINR: round2(grossAmountInINR), fees: { transferFee, platformFee, gst, totalFees }, exchangeLoss, netAmountReceived, summary: { senderPays: `${fromMeta.symbol}${amount.toFixed(2)}`, receiverGets: `${toMeta.symbol}${netAmountReceived.toFixed(2)}` }, meta: { rateProvider: 'external-live-rate-api', cached: live.cached, timestamp: live.timestamp } };
    } catch (error) {
      console.error(' Failed', error && error.message ? error.message : error);
      throw error;
    }
  }
}

module.exports = { CurrencyTransferService, SUPPORTED_CURRENCIES, validatePayload };