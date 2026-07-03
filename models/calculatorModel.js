const { getExchangeRates } = require('./exchangeRateModel');

const round2 = (value) => Math.round((Number(value) + Number.EPSILON) * 100) / 100;

const calculateTransferQuote = async ({ fromCurrency, toCurrency, amount }) => {
  const rates = await getExchangeRates(fromCurrency);
  const marketRate = Number(rates.rates[toCurrency] || 0);
  const bankRate = round2(marketRate * 0.99);
  const grossAmount = round2(amount * marketRate);
  const bankAmount = round2(amount * bankRate);
  const transferFee = round2(Math.max(0.5, amount * 0.005));
  const platformFee = round2(Math.max(0.2, amount * 0.002));
  const gst = round2((transferFee + platformFee) * 0.18);
  const totalFees = round2(transferFee + platformFee + gst);
  const netAmountReceived = round2(grossAmount - totalFees);
  const exchangeLoss = round2(grossAmount - bankAmount);
  const gainLossVsMarketRate = round2(netAmountReceived - grossAmount);

  return { marketRate, bankRate, grossAmount, bankAmount, transferFee, platformFee, gst, totalFees, netAmountReceived, exchangeLoss, gainLossVsMarketRate };
};

module.exports = { calculateTransferQuote };
