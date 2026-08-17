const budgetModel = require('../models/budgetModel');

exports.createBudget = async (req, res) => {
  try {
    const result = await budgetModel.createBudget(req.body);
    console.log(' Connected');
    return res.status(201).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};

exports.getBudgetById = async (req, res) => {
  try {
    const result = await budgetModel.getBudgetById(req.params.id);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};
