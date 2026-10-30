require('dotenv').config();
const { createApp } = require('./app');
const { createLogger } = require('./utils/logger');

const logger = createLogger();
const app = createApp({ logger });

const port = Number(process.env.PORT || 8080);
app.listen(port, () => {
  logger.info({ port, service: 'combined_project' }, 'server_started');
});
