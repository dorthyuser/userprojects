const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true }));

app.get('/health', (req, res) => {
  try {
    return res.status(200).json({ status: 'ok', message: 'Connected' });
  } catch (error) {
    console.error('Failed', error.message);
    return res.status(500).json({ error: { code: 'INTERNAL_SERVER_ERROR', message: 'Failed' } });
  }
});

app.use('/', routes);

app.use((err, req, res, next) => {
  try {
    console.error('Failed', err && err.message ? err.message : 'Unknown error');
    return res.status(500).json({
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'Failed'
      }
    });
  } catch (error) {
    console.error('Failed', error.message);
    return res.status(500).json({
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'Failed'
      }
    });
  }
});

app.listen(PORT, () => {
  try {
    console.log('Connected');
  } catch (error) {
    console.error('Failed', error.message);
  }
});
