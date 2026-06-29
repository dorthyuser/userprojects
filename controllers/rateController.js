const { getExchangeRates } = require('../models/rateModel');

const getRates = async (req, res) => {
  try {
    const baseCurrency = String(req.query.baseCurrency || 'USD').toUpperCase();
    const result = await getExchangeRates(baseCurrency);
    return res.status(200).json(result);
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(400).json({
      error: {
        code: 'RATES_FETCH_FAILED',
        message: error.message || 'Unable to fetch exchange rates',
        details: null
      }
    });
  }
};

module.exports = { getRates };
