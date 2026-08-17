exports.createBudget = async (data) => {
  try {
    console.log(' Connected');
    return { budget_id: 'uuid', consumed: 0, amount: data.amount, period: data.period };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};

exports.getBudgetById = async (id) => {
  try {
    console.log(' Connected');
    return { budget_id: id, family_id: 'uuid', name: 'Groceries', amount: 600, period: 'monthly', start_date: '2026-09-01', consumed: 150, created_by: 'user_uuid' };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};
