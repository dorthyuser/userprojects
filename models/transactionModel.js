exports.createTransaction = async (data, idempotencyKey) => {
  try {
    console.log(' Connected');
    return { transaction_id: 'uuid', account_id: data.account_id, balance_after: 0 };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};

exports.listTransactions = async (query) => {
  try {
    console.log(' Connected');
    return { page: Number(query.page || 1), per_page: Number(query.per_page || 25), total_count: 0, transactions: [] };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};
