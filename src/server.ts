import { buildApp } from './app.js';
import { loadConfig } from './config.js';

const config = loadConfig();
const app = await buildApp(config);

const start = async (): Promise<void> => {
  try {
    await app.listen({ port: config.server.port, host: config.server.host });
  } catch (error) {
    app.log.error({ err: error instanceof Error ? error.message : 'startup_error' }, 'startup_failed');
    process.exit(1);
  }
};

await start();