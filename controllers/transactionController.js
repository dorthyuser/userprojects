const transactionModel = require('../models/transactionModel');

exports.createTransaction = async (req, res) => {
  try {
    const result = await transactionModel.createTransaction(req.body, req.headers['idempotency-key']);
    console.log(' Connected');
    return res.status(201).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};

exports.listTransactions = async (req, res) => {
  try {
    const result = await transactionModel.listTransactions(req.query);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};
