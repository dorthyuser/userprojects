exports.createFamily = async (data) => {
  try {
    console.log(' Connected');
    return { family_id: 'uuid', name: data.name, created_by: 'user_uuid' };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};

exports.inviteMember = async (familyId, data) => {
  try {
    console.log(' Connected');
    return { invite_id: 'uuid', family_id: familyId, email: data.email, status: 'invited' };
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    throw { statusCode: 500, code: 'SERVER_ERROR', publicMessage: 'Unexpected failure', details: {} };
  }
};
