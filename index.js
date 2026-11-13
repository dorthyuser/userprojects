const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');
const { postgresqlConnection } = require('./connections/postgresql');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

try {
  if (postgresqlConnection) {
    console.log('PostgreSql Connected');
  } else {
    console.log('PostgreSql Failed');
  }
} catch (error) {
  console.log('PostgreSql Failed', error && error.message ? error.message : 'Connection error');
}

app.use('/', routes);

app.use((err, req, res, next) => {
  console.error('Unhandled error');
  res.status(500).json({
    error: {
      code: 'INTERNAL_SERVER_ERROR',
      message: 'An unexpected error occurred'
    }
  });
});

app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});
