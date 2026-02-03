try {
  require('dotenv').config();
  const express = require('express');
  const cors = require('cors');

  // Initialize connections (must be imported/executed on startup)
  const salesforceConnectionModule = require('./connections/salesforce');

  const routes = require('./routes');

  const app = express();

  // Global CORS config (all origins, all methods)
  app.use(cors());
  app.use(express.json());

  app.use('/api', routes);

  const PORT = process.env.PORT || 8080;

  app.listen(PORT, () => {
    console.log(`Server started on port ${PORT}`);
  });
  console.log('Connected index.js');
} catch (error) {
  console.log('Failed index.js', error);
}
