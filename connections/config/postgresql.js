const dbOptions = require('../../config/postgresql/DbOptions');

try {
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', error.message);
}

module.exports = dbOptions;
