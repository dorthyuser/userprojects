exports.register = async (data) => {
  try {
    console.log(' Connected');
    return { user_id: 'uuid', email: data.email, name: data.name || null, token: 'jwt' };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};

exports.login = async (data) => {
  try {
    console.log(' Connected');
    return { user_id: 'uuid', token: 'jwt', expires_in: 3600 };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};
