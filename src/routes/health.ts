import type { FastifyInstance } from 'fastify';

export async function createHealthRoutes(app: FastifyInstance) {
  app.get('/health', async () => ({ status: 'ok', service: 'heelell' }));
}