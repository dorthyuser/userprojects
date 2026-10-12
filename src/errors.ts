import type { FastifyError, FastifyReply, FastifyRequest } from 'fastify';

export function createErrorHandler(logger: { error: (obj: unknown, msg?: string) => void }) {
  return async (error: FastifyError, _request: FastifyRequest, reply: FastifyReply) => {
    logger.error({ err: error.message }, 'request_failed');
    const statusCode = error.statusCode && error.statusCode >= 400 && error.statusCode < 600 ? error.statusCode : 500;
    reply.status(statusCode).send({ message: statusCode === 500 ? 'An unexpected error occurred.' : error.message });
  };
}