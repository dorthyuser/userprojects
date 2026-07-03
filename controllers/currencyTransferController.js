const { getSupportedCurrencies, getCurrencyMeta, isValidCurrencyCode } = require('../models/currencyModel');
const { getExchangeRates } = require('../models/exchangeRateModel');
const { calculateTransferQuote } = require('../models/calculatorModel');
const { createStructuredError } = require('../middleware/errorHandler');

const round2 = (value) => Math.round((Number(value) + Number.EPSILON) * 100) / 100;

const healthCheck = async (req, res, next) => {
  try {
    const rates = await getExchangeRates('USD');
    return res.status(200).json({
      status: 'ok',
      service: 'currency-transfer-api',
      dependencies: {
        exchangeRateProvider: 'ok',
        cache: rates.cached ? 'ok' : 'ok'
      },
      timestamp: new Date().toISOString()
    });
  } catch (error) {
    return next(createStructuredError(503, 'SERVICE_UNAVAILABLE', 'Health check failed'));
  }
};

const getCurrencies = async (req, res, next) => {
  try {
    return res.status(200).json({ currencies: getSupportedCurrencies() });
  } catch (error) {
    return next(createStructuredError(500, 'INTERNAL_ERROR', 'Unable to fetch currencies'));
  }
};

const getRates = async (req, res, next) => {
  try {
    const baseCurrency = String(req.query.baseCurrency || 'USD').toUpperCase();
    if (!isValidCurrencyCode(baseCurrency)) {
      return next(createStructuredError(400, 'VALIDATION_ERROR', 'Invalid baseCurrency'));
    }
    const rates = await getExchangeRates(baseCurrency);
    return res.status(200).json(rates);
  } catch (error) {
    return next(createStructuredError(502, 'EXCHANGE_RATE_ERROR', 'Unable to fetch exchange rates'));
  }
};

const calculateTransfer = async (req, res, next) => {
  try {
    const fromCurrency = String(req.body.fromCurrency || '').toUpperCase();
    const toCurrency = String(req.body.toCurrency || '').toUpperCase();
    const amount = Number(req.body.amount);

    if (!isValidCurrencyCode(fromCurrency) || !isValidCurrencyCode(toCurrency)) {
      return next(createStructuredError(400, 'VALIDATION_ERROR', 'Invalid currency code'));
    }
    if (!Number.isFinite(amount) || amount <= 0) {
      return next(createStructuredError(400, 'VALIDATION_ERROR', 'Amount must be a positive number'));
    }

    const quote = await calculateTransferQuote({ fromCurrency, toCurrency, amount });
    return res.status(200).json({
      source: {
        currency: fromCurrency,
        symbol: getCurrencyMeta(fromCurrency).symbol,
        amount: round2(amount)
      },
      destination: {
        currency: toCurrency,
        symbol: getCurrencyMeta(toCurrency).symbol
      },
      marketRate: round2(quote.marketRate),
      bankRate: round2(quote.bankRate),
      grossAmountInINR: round2(quote.grossAmount),
      fees: {
        transferFee: round2(quote.transferFee),
        platformFee: round2(quote.platformFee),
        gst: round2(quote.gst),
        totalFees: round2(quote.totalFees)
      },
      exchangeLoss: round2(quote.exchangeLoss),
      netAmountReceived: round2(quote.netAmountReceived),
      summary: {
        senderPays: `${getCurrencyMeta(fromCurrency).symbol}${round2(amount).toFixed(2)}`,
        receiverGets: `${getCurrencyMeta(toCurrency).symbol}${round2(quote.netAmountReceived).toFixed(2)}`
      },
      breakdown: {
        originalAmount: round2(amount),
        convertedAtMarketRate: round2(quote.grossAmount),
        convertedAtBankRate: round2(quote.bankAmount),
        transferFee: round2(quote.transferFee),
        platformFee: round2(quote.platformFee),
        gstOnFees: round2(quote.gst),
        totalDeductions: round2(quote.totalFees),
        finalAmountReceived: round2(quote.netAmountReceived),
        gainLossVsMarketRate: round2(quote.gainLossVsMarketRate)
      }
    });
  } catch (error) {
    return next(createStructuredError(500, 'CALCULATION_ERROR', 'Unable to calculate transfer quote'));
  }
};

module.exports = { healthCheck, getCurrencies, getRates, calculateTransfer };
