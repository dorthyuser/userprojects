const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');
const { initializePostgreSqlConnection } = require('./connections/postgresql');

try {
  dotenv.config();
  const app = express();
  app.use(cors({ origin: '*', methods: ['GET', 'POST', 'DELETE', 'OPTIONS'] }));
  app.use(express.json({ limit: '1mb' }));

  initializePostgreSqlConnection();

  app.use('/api', routes);

  app.get('/health', (req, res) => {
    try {
      res.status(200).json({ status: 'ok' });
    } catch (error) {
      console.error(' Failed', error.message);
      res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'Unexpected error' } });
    }
  });

  const port = process.env.PORT || 8080;
  app.listen(port, () => {
    console.log(' Connected');
  });
} catch (error) {
  console.error(' Failed', error.message);
}
