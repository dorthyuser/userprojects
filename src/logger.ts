export function createLogger(level: string) {
  return {
    level,
    redact: {
      paths: ['req.headers.authorization', 'req.headers.client_id', 'req.body.travelcardNumber', 'req.body.travelcardTransactionReference', 'req.body.cardholders.*.cardholderPhotoURL', 'req.body.cardholders.*.cardholderPhotoKey', 'req.body.cardholders.*.cardholderPhotoRRSKey', 'res.body.token'],
      censor: '[REDACTED]'
    }
  } as const;
}