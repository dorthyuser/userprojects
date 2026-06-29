const { calculateCurrencyTransfer } = require('../models/calculateModel');

const calculateTransfer = async (req, res) => {
  try {
    const { fromCurrency, toCurrency, amount } = req.body || {};
    const result = await calculateCurrencyTransfer({ fromCurrency, toCurrency, amount });
    return res.status(200).json(result);
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(400).json({
      error: {
        code: 'CALCULATION_FAILED',
        message: error.message || 'Unable to calculate transfer',
        details: null
      }
    });
  }
};

module.exports = { calculateTransfer };
