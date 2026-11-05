const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true }));

try {
  require('./connections/mysql');
  console.log('mysql Connected');
} catch (error) {
  console.error('mysql Failed', error && error.message ? error.message : error);
}

try {
  app.use('/', require('./routes'));
} catch (error) {
  console.error('routes Failed', error && error.message ? error.message : error);
}

app.use((err, req, res, next) => {
  console.error('Unhandled Failed', err && err.message ? err.message : err);
  res.status(500).json({
    error: {
      code: 'INTERNAL_SERVER_ERROR',
      message: 'An unexpected error occurred.'
    }
  });
});

app.listen(PORT, () => {
  console.log(`Connected on port ${PORT}`);
});
