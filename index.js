const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');
const errorHandler = require('./middleware/errorHandler');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json({ limit: '10kb' }));
app.use(express.urlencoded({ extended: true }));

app.use('/api', routes);
app.use(errorHandler);

app.listen(PORT, () => {
  try {
    console.log(' Connected');
  } catch (error) {
    console.error(' Failed', { code: error.code || 'STARTUP_ERROR' });
  }
});
