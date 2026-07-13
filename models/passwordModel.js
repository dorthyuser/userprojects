try {
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message, code: error.code || 'MODEL_INIT_ERROR' });
}

module.exports = {};
