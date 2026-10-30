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
} catch (error) {
}

try {
  app.use('/api', require('./routes'));
} catch (error) {
}

try {
  app.get('/', (req, res) => {
    res.status(200).json({ message: 'API is running' });
  });
} catch (error) {
}

try {
  app.listen(PORT, () => {
  });
} catch (error) {
}
