const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json());

try {
  require('./connections/postgresql');
  console.log('postgresql Connected');
} catch (error) {
  console.error('postgresql Failed', error && error.message ? error.message : 'Connection error');
}

const routes = require('./routes');
app.use('/', routes);

app.use((err, req, res, next) => {
  console.error('Request Failed', err && err.message ? err.message : 'Internal error');
  res.status(500).json({
    error: {
      code: 'INTERNAL_SERVER_ERROR',
      message: 'An internal error occurred.'
    }
  });
});

app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});
