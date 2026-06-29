const getHealth = async (req, res) => {
  try {
    return res.status(200).json({
      status: 'ok',
      service: 'currency-transfer-calculator',
      dependencies: {
        exchangeRateProvider: 'ok',
        cache: 'ok'
      }
    });
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(500).json({
      error: {
        code: 'HEALTH_CHECK_FAILED',
        message: 'Health check failed',
        details: null
      }
    });
  }
};

module.exports = { getHealth };
