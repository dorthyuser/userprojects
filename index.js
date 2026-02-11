const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
try {
  dotenv.config();
  const app = express();
  app.use(cors({ origin: '*', methods: ['GET','POST','PUT','PATCH','DELETE','OPTIONS'] }));
  app.use(express.json());
  const routes = require('./routes');
  app.use('/api', routes);
  const PORT = process.env.BACKEND_PORT || process.env.PORT || 8080;
  app.listen(PORT, () => {
    console.log(' Connected');
    console.log(`Server running on port ${PORT}`);
  });
} catch (error) {
  console.log(' Failed', error);
}
