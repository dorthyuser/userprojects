exports.createAccount = async (data) => {
  try {
    console.log(' Connected');
    return { account_id: 'uuid', balance: data.initial_balance || 0, currency: data.currency };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};
