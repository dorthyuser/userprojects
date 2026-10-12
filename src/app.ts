import Fastify from 'fastify';
import helmet from '@fastify/helmet';
import rateLimit from '@fastify/rate-limit';
import swagger from '@fastify/swagger';
import swaggerUi from '@fastify/swagger-ui';
import { loadConfig, type AppConfig } from './config.js';
import { createTravelcardRoutes } from './routes/travelcards.js';
import { createHealthRoutes } from './routes/health.js';
import { createErrorHandler } from './errors.js';
import { createLogger } from './logger.js';
import { Database } from './db/database.js';
import { SecretsService } from './services/secrets.js';

export async function buildApp(config: AppConfig = loadConfig()) {
  const app = Fastify({
    logger: createLogger(config.app.logLevel)
  });

  if (config.security.helmet) {
    await app.register(helmet, { hidePoweredBy: config.security.hidePoweredBy });
  }

  await app.register(rateLimit, {
    max: config.security.rateLimit.max,
    timeWindow: config.security.rateLimit.timeWindowMs
  });

  await app.register(swagger, {
    openapi: {
      info: {
        title: 'heelell API',
        version: '1.0.0'
      }
    }
  });

  await app.register(swaggerUi, {
    routePrefix: config.api.docsPath
  });

  const secrets = new SecretsService(config.aws);
  const database = new Database(config.database, secrets, app.log);

  app.setErrorHandler(createErrorHandler(app.log));

  await app.register(createHealthRoutes, { prefix: '' });
  await app.register(createTravelcardRoutes, { prefix: config.api.basePath, database });

  app.addHook('onClose', async () => {
    await database.close();
  });

  return app;
}