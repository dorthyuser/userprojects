const { readRecords, writeRecords } = require('../Helpers/db');

const saveLifetimeCalculation = async (record) => {
  const records = readRecords();
  records.push({
    id: records.length + 1,
    ...record,
    createdAt: new Date().toISOString()
  });
  writeRecords(records);
  return true;
};

module.exports = { saveLifetimeCalculation };
