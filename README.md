# paymentcsharp441

.NET 8 AWS Lambda service for payment initiation, verification, and retrieval backed by PostgreSQL.

## Environment variables
- AWS_SECRET_NAME
- host
- port
- dbname
- username
- password
- TAX_RATE_PERCENT
- IDEMPOTENCY_WINDOW_SEC
- GATEWAY_RETRY_COUNT
- DEFAULT_CURRENCY
- LOG_LEVEL

## Secrets Manager
Store a JSON secret with keys:
- host
- port
- dbname
- username
- password

## Build and deploy
1. Restore packages.
2. Build the project for net8.0.
3. Deploy using AWS Lambda tooling.

## Local run
- Set the environment variables above.
- Provide PostgreSQL connectivity.
- Invoke the Lambda handler with API Gateway proxy events.

## Notes
- Raw SQL only.
- Parameterized queries only.
- No sensitive data is logged.
- Payment verification and invoice creation are intended to run atomically within PostgreSQL transactions.
