const authModel = require('../models/authModel');

exports.register = async (req, res) => {
  try {
    const result = await authModel.register(req.body);
    console.log(' Connected');
    return res.status(201).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};

exports.login = async (req, res) => {
  try {
    const result = await authModel.login(req.body);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};
