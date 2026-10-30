const fs = require('fs');
const path = require('path');

const dataDir = path.join(__dirname, '..', 'data');
const dataFile = path.join(dataDir, 'lifetime-calculations.json');

const ensureStorage = () => {
  if (!fs.existsSync(dataDir)) {
    fs.mkdirSync(dataDir, { recursive: true });
  }

  if (!fs.existsSync(dataFile)) {
    fs.writeFileSync(dataFile, '[]', 'utf8');
  }
};

const readRecords = () => {
  ensureStorage();
  const content = fs.readFileSync(dataFile, 'utf8');
  return JSON.parse(content);
};

const writeRecords = (records) => {
  ensureStorage();
  fs.writeFileSync(dataFile, JSON.stringify(records, null, 2), 'utf8');
};

module.exports = { readRecords, writeRecords };
