import fs from 'node:fs';
import path from 'node:path';

export type AppConfig = {
  server: { port: number; host: string };
  app: { name: string; environment: string; logLevel: string };
  security: { rateLimit: { max: number; timeWindowMs: number }; helmet: boolean; hidePoweredBy: boolean };
  aws: { region: string; secretsManagerSecretName: string };
  database: { host: string; port: number; name: string; user: string; password: string; ssl: boolean };
  api: { basePath: string; createPath: string; healthPath: string; docsPath: string };
};

const defaultConfig: AppConfig = {
  server: { port: Number(process.env.PORT ?? 3000), host: process.env.HOST ?? '0.0.0.0' },
  app: { name: 'heelell', environment: process.env.NODE_ENV ?? 'development', logLevel: process.env.LOG_LEVEL ?? 'info' },
  security: { rateLimit: { max: Number(process.env.RATE_LIMIT_MAX ?? 100), timeWindowMs: Number(process.env.RATE_LIMIT_WINDOW_MS ?? 60000) }, helmet: true, hidePoweredBy: true },
  aws: { region: process.env.AWS_REGION ?? 'us-east-1', secretsManagerSecretName: process.env.AWS_SECRET_NAME ?? '' },
  database: { host: process.env.POSTGRESQLHOST ?? '', port: Number(process.env.POSTGRESQLPORT ?? 5432), name: process.env.POSTGRESQLDATABASE ?? '', user: process.env.POSTGRESQLUSERNAME ?? '', password: process.env.POSTGRESQLPASSWORD ?? '', ssl: String(process.env.DATABASE_SSL ?? 'false').toLowerCase() === 'true' },
  api: { basePath: '/travelcards', createPath: '/', healthPath: '/health', docsPath: '/docs' }
};

export function loadConfig(): AppConfig {
  const filePath = path.resolve(process.cwd(), 'application.yml');
  if (!fs.existsSync(filePath)) return defaultConfig;
  return defaultConfig;
}