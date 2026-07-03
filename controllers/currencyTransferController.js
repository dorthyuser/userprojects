const { CurrencyTransferService } = require('../models/currencyTransferModel');

const service = new CurrencyTransferService();

function safeError(res, error, statusCode = 500) {
  return res.status(statusCode).json({
    error: {
      code: statusCode === 400 ? 'VALIDATION_ERROR' : 'INTERNAL_ERROR',
      message: statusCode === 400 ? error.message : 'An unexpected error occurred',
      requestId: res.locals.requestId || null
    }
  });
}

exports.health = async (req, res) => {
  try {
    const result = await service.health();
    console.log(' Connected');
    return res.json(result);
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    return safeError(res, error);
  }
};

exports.getCurrencies = async (req, res) => {
  try {
    const result = await service.getCurrencies();
    console.log(' Connected');
    return res.json(result);
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    return safeError(res, error);
  }
};

exports.getRates = async (req, res) => {
  try {
    const result = await service.getRates(req.query.baseCurrency || 'USD');
    console.log(' Connected');
    return res.json(result);
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    return safeError(res, error, 400);
  }
};

exports.getSwagger = async (req, res) => {
  try {
    const result = await service.getSwagger();
    console.log(' Connected');
    return res.json(result);
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    return safeError(res, error);
  }
};

exports.validateTransfer = async (req, res) => {
  try {
    const result = await service.validateTransfer(req.body || {});
    console.log(' Connected');
    return res.json(result);
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    return safeError(res, error, 400);
  }
};

exports.calculateTransfer = async (req, res) => {
  try {
    const result = await service.calculateTransfer(req.body || {});
    console.log(' Connected');
    return res.json(result);
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    return safeError(res, error, 400);
  }
};