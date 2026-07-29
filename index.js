const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

try {
  require('./connections/postgresql');
  console.log('postgresql Connected');
} catch (error) {
  console.error('postgresql Failed', error);
}

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true }));

app.use('/api/v1', require('./routes'));

app.use((err, req, res, next) => {
  const statusCode = err.statusCode || 500;
  const code = err.code || 'INTERNAL_SERVER_ERROR';
  const message = statusCode >= 500 ? 'Unexpected server error' : (err.message || 'Request failed');
  const details = err.details && typeof err.details === 'object' ? err.details : {};
  res.status(statusCode).json({ error: { code, message, details } });
});

app.listen(PORT, () => {
  console.log(`Server Connected on port ${PORT}`);
});
