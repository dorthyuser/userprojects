try {
  const dotenv = require('dotenv');
  dotenv.config();
  const express = require('express');
  const cors = require('cors');
  const routes = require('./routes');
  const app = express();
  app.use(cors());
  app.use(express.json());
  app.use('/api', routes);
  app.use((err, req, res, next) => {
    console.error(' Failed', err);
    res.status(err.status || 500).json({ error: { message: err.message || 'Internal Server Error', code: err.code || 'SERVER_ERROR', details: err.details || null } });
  });
  const PORT = process.env.PORT || process.env.BACKEND_PORT || 8080;
  app.listen(PORT, () => {
    console.log(' Connected');
  });
} catch (err) {
  console.error(' Failed', err);
}
