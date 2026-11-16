const model = require('../models/travelCardModel');

const structuredError = (res, error) => {
  console.error(' Failed', { message: error.message });
  return res.status(error.statusCode || 500).json({
    error: {
      code: error.code || 'INTERNAL_ERROR',
      message: error.publicMessage || 'Unexpected error'
    }
  });
};

exports.createTravelCard = async (req, res) => {
  try {
    const result = await model.createTravelCard(req.body);
    console.log(' Connected');
    return res.status(201).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.getTravelCard = async (req, res) => {
  try {
    const result = await model.getTravelCard(req.params.travelCardId);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.updateTravelCard = async (req, res) => {
  try {
    const result = await model.updateTravelCard(req.params.travelCardId, req.body);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.deleteTravelCard = async (req, res) => {
  try {
    const result = await model.deleteTravelCard(req.params.travelCardId);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.activateTravelCard = async (req, res) => {
  try {
    const result = await model.activateTravelCard(req.params.travelCardId, req.body);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.freezeTravelCard = async (req, res) => {
  try {
    const result = await model.freezeTravelCard(req.params.travelCardId, req.body);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.unfreezeTravelCard = async (req, res) => {
  try {
    const result = await model.unfreezeTravelCard(req.params.travelCardId, req.body);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.updateTravelNotice = async (req, res) => {
  try {
    const result = await model.updateTravelNotice(req.params.travelCardId, req.body);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};

exports.getTravelCardTransactions = async (req, res) => {
  try {
    const result = await model.getTravelCardTransactions(req.params.travelCardId);
    console.log(' Connected');
    return res.status(200).json(result);
  } catch (error) {
    return structuredError(res, error);
  }
};
