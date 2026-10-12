import type { FastifyInstance } from 'fastify';
import { createTravelcardSchema } from '../validation.js';
import type { Database } from '../db/database.js';

export async function createTravelcardRoutes(app: FastifyInstance, opts: { database: Database }) {
  app.post('/', async (request, reply) => {
    const parsed = createTravelcardSchema.safeParse(request.body);
    if (!parsed.success) {
      return reply.status(400).send({ message: 'Invalid request body.' });
    }
    const result = await opts.database.createTravelcard(parsed.data);
    return reply.status(201).send(result);
  });
}