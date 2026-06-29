const { currencyMap } = require('./currencyModel');
const { getExchangeRates } = require('./rateModel');

const round2 = (n) => Math.round((Number(n) + Number.EPSILON) * 100) / 100;

const calculateCurrencyTransfer = async ({ fromCurrency, toCurrency, amount }) => {
  if (!currencyMap[fromCurrency] || !currencyMap[toCurrency]) throw new Error('Invalid currency code');
  if (typeof amount !== 'number' || amount <= 0) throw new Error('Amount must be a positive number');

  const rates = await getExchangeRates(fromCurrency);
  const marketRate = Number(rates.rates[toCurrency] || 0);
  if (!marketRate) throw new Error('Exchange rate unavailable for selected currency pair');

  const bankRate = round2(marketRate * 0.99);
  const grossAmount = round2(amount * marketRate);
  const transferFee = round2(Math.max(0.5, amount * 0.5));
  const platformFee = round2(Math.max(0.2, amount * 0.2));
  const gst = round2((transferFee + platformFee) * 0.18);
  const totalFees = round2(transferFee + platformFee + gst);
  const exchangeLoss = round2((marketRate - bankRate) * amount);
  const netAmountReceived = round2(grossAmount - totalFees - exchangeLoss);

  return {
    source: { currency: fromCurrency, symbol: currencyMap[fromCurrency].symbol, amount: round2(amount) },
    destination: { currency: toCurrency, symbol: currencyMap[toCurrency].symbol },
    marketRate: round2(marketRate),
    bankRate: round2(bankRate),
    grossAmountInINR: round2(grossAmount),
    fees: { transferFee, platformFee, gst, totalFees },
    exchangeLoss,
    netAmountReceived,
    summary: { senderPays: `${currencyMap[fromCurrency].symbol}${round2(amount).toFixed(2)}`, receiverGets: `${currencyMap[toCurrency].symbol}${netAmountReceived.toFixed(2)}` },
    breakdown: {
      originalAmount: round2(amount),
      currentMarketExchangeRate: round2(marketRate),
      bankExchangeRate: round2(bankRate),
      transferFee,
      platformFee,
      gstOrTax: gst,
      totalDeductions: totalFees,
      amountReceivedAfterDeductions: netAmountReceived,
      exchangeGainLossComparedToMarketRate: exchangeLoss
    }
  };
};

module.exports = { calculateCurrencyTransfer };
