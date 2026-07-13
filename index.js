const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json({ limit: '10kb' }));
app.use(express.urlencoded({ extended: true }));

app.use('/api', routes);

app.use((err, req, res, next) => {
  try {
    console.error(' Failed', { message: err.message, code: err.code || 'INTERNAL_ERROR' });
    res.status(err.statusCode || 500).json({
      success: false,
      error: {
        code: err.code || 'INTERNAL_ERROR',
        message: err.publicMessage || 'An unexpected error occurred',
        details: err.details || null,
        timestamp: new Date().toISOString()
      }
    });
  } catch (error) {
    console.error(' Failed', { message: error.message, code: error.code || 'INTERNAL_ERROR' });
    res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'An unexpected error occurred',
        timestamp: new Date().toISOString()
      }
    });
  }
});

app.listen(PORT, () => {
  try {
    console.log(' Connected');
  } catch (error) {
    console.error(' Failed', { message: error.message, code: error.code || 'STARTUP_ERROR' });
  }
});
