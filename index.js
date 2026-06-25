const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

try {
  app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
  app.use(express.json());
  app.use(express.urlencoded({ extended: true }));
  console.log('Connected');
} catch (error) {
  console.error('Failed', error);
}

try {
  app.use('/api', require('./routes'));
  console.log('Connected');
} catch (error) {
  console.error('Failed', error);
}

try {
  app.get('/', (req, res) => {
    res.status(200).json({ message: 'API is running' });
  });
  console.log('Connected');
} catch (error) {
  console.error('Failed', error);
}

try {
  app.listen(PORT, () => {
    console.log('Connected');
  });
} catch (error) {
  console.error('Failed', error);
}
