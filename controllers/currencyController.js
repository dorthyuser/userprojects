const { supportedCurrencies } = require('../models/currencyModel');

const getSupportedCurrencies = async (req, res) => {
  try {
    return res.status(200).json({ supportedCurrencies });
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(500).json({
      error: {
        code: 'CURRENCIES_FETCH_FAILED',
        message: 'Unable to fetch supported currencies',
        details: null
      }
    });
  }
};

module.exports = { getSupportedCurrencies };
