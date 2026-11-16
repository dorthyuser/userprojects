const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'] }));
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true }));

app.use('/api', routes);

app.get('/health', (req, res) => {
  try {
    console.log(' Connected');
    return res.status(200).json({ status: 'ok' });
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'Unexpected error' } });
  }
});

app.use((err, req, res, next) => {
  try {
    console.error(' Failed', { message: err.message });
    return res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'Unexpected error' } });
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'Unexpected error' } });
  }
});

app.listen(PORT, () => {
  try {
    console.log(' Connected');
  } catch (error) {
    console.error(' Failed', { message: error.message });
  }
});
