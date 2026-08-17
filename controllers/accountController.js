const accountModel = require('../models/accountModel');

exports.createAccount = async (req, res) => {
  try {
    const result = await accountModel.createAccount(req.body);
    console.log(' Connected');
    return res.status(201).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};
