const crypto = require('crypto');

const db = new Map();

const now = () => new Date().toISOString();
const id = () => `tc_${crypto.randomBytes(8).toString('hex')}`;
const maskCard = (value) => value ? `**** **** **** ${String(value).slice(-4)}` : '**** **** **** 1234';

const validateId = (travelCardId) => {
  if (!travelCardId || typeof travelCardId !== 'string') {
    const error = new Error('Invalid travelCardId');
    error.code = 'VALIDATION_ERROR';
    error.statusCode = 400;
    error.publicMessage = 'Invalid request';
    throw error;
  }
};

exports.createTravelCard = async (payload) => {
  try {
    if (!payload || !payload.customerId || !payload.cardType || !payload.currency) {
      const error = new Error('Missing required fields');
      error.code = 'VALIDATION_ERROR';
      error.statusCode = 400;
      error.publicMessage = 'Invalid request';
      throw error;
    }
    const travelCardId = id();
    const record = {
      travelCardId,
      status: 'ACTIVE',
      maskedCardNumber: maskCard(),
      expiryMonth: 12,
      expiryYear: 2030,
      createdAt: now(),
      updatedAt: now(),
      spendControls: payload.spendControls || { dailyLimit: 0, monthlyLimit: 0, merchantCategoryBlocks: [] },
      travelNotice: payload.travelNotice || { startDate: '', endDate: '', destinations: [] },
      transactions: []
    };
    db.set(travelCardId, record);
    return {
      travelCardId: record.travelCardId,
      status: record.status,
      maskedCardNumber: record.maskedCardNumber,
      expiryMonth: record.expiryMonth,
      expiryYear: record.expiryYear,
      createdAt: record.createdAt
    };
  } catch (error) {
    throw error;
  }
};

exports.getTravelCard = async (travelCardId) => {
  try {
    validateId(travelCardId);
    const record = db.get(travelCardId) || { travelCardId, status: 'ACTIVE', maskedCardNumber: maskCard(), expiryMonth: 12, expiryYear: 2030, spendControls: { dailyLimit: 0, monthlyLimit: 0, merchantCategoryBlocks: [] }, travelNotice: { startDate: '', endDate: '', destinations: [] }, createdAt: now(), updatedAt: now() };
    return {
      travelCardId: record.travelCardId,
      status: record.status,
      maskedCardNumber: record.maskedCardNumber,
      expiryMonth: record.expiryMonth,
      expiryYear: record.expiryYear,
      currency: 'USD',
      spendControls: record.spendControls,
      travelNotice: record.travelNotice,
      createdAt: record.createdAt,
      updatedAt: record.updatedAt
    };
  } catch (error) {
    throw error;
  }
};

exports.updateTravelCard = async (travelCardId, payload) => {
  try {
    validateId(travelCardId);
    const record = db.get(travelCardId) || { travelCardId, status: 'ACTIVE', maskedCardNumber: maskCard(), expiryMonth: 12, expiryYear: 2030, spendControls: {}, travelNotice: {}, createdAt: now(), updatedAt: now(), transactions: [] };
    record.spendControls = payload.spendControls || record.spendControls;
    record.travelNotice = payload.travelNotice || record.travelNotice;
    record.status = payload.status || record.status;
    record.updatedAt = now();
    db.set(travelCardId, record);
    return { travelCardId: record.travelCardId, status: record.status, updatedAt: record.updatedAt };
  } catch (error) {
    throw error;
  }
};

exports.deleteTravelCard = async (travelCardId) => {
  try {
    validateId(travelCardId);
    db.delete(travelCardId);
    return { travelCardId, status: 'DELETED', deletedAt: now() };
  } catch (error) {
    throw error;
  }
};

exports.activateTravelCard = async (travelCardId) => {
  try {
    validateId(travelCardId);
    return { travelCardId, status: 'ACTIVE', activatedAt: now() };
  } catch (error) {
    throw error;
  }
};

exports.freezeTravelCard = async (travelCardId) => {
  try {
    validateId(travelCardId);
    return { travelCardId, status: 'FROZEN', frozenAt: now() };
  } catch (error) {
    throw error;
  }
};

exports.unfreezeTravelCard = async (travelCardId) => {
  try {
    validateId(travelCardId);
    return { travelCardId, status: 'ACTIVE', unfrozenAt: now() };
  } catch (error) {
    throw error;
  }
};

exports.updateTravelNotice = async (travelCardId, payload) => {
  try {
    validateId(travelCardId);
    return { travelCardId, travelNotice: { startDate: payload.startDate || '', endDate: payload.endDate || '', destinations: payload.destinations || [] }, updatedAt: now() };
  } catch (error) {
    throw error;
  }
};

exports.getTravelCardTransactions = async (travelCardId) => {
  try {
    validateId(travelCardId);
    return {
      travelCardId,
      transactions: [
        {
          transactionId: 'txn_001',
          postedAt: now(),
          amount: 125.5,
          currency: 'USD',
          merchantName: 'Sample Merchant',
          merchantCategoryCode: '4511',
          status: 'POSTED',
          isInternational: false,
          isFraudFlagged: false
        }
      ]
    };
  } catch (error) {
    throw error;
  }
};
