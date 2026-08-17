const familyModel = require('../models/familyModel');

exports.createFamily = async (req, res) => {
  try {
    const result = await familyModel.createFamily(req.body);
    console.log(' Connected');
    return res.status(201).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};

exports.inviteMember = async (req, res) => {
  try {
    const result = await familyModel.inviteMember(req.params.family_id, req.body);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    return res.status(error.statusCode || 500).json({ error: { code: error.code || 'SERVER_ERROR', message: error.publicMessage || 'Unexpected failure', details: error.details || {} } });
  }
};
