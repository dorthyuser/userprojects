# Paymentcsharp441Lambda

.NET 8 AWS Lambda service for payment initiation, verification, and retrieval backed by PostgreSQL.

## Environment variables
- `AWS_SECRET_NAME` - Secrets Manager secret name containing DB and gateway credentials
- `host`
- `port`
- `dbname`
- `username`
- `password`
- `gateway_secret`
- `LOG_LEVEL` - optional, defaults to `INFO`
- `IDEMPOTENCY_WINDOW_SEC` - optional, defaults to `120`
- `GATEWAY_RETRY_COUNT` - optional, defaults to `2`
- `DEFAULT_CURRENCY` - optional, defaults to `INR`
- `TAX_RATE_PERCENT` - optional, defaults to `18`

## Deployment
1. Provision PostgreSQL and create the schema, sequences, indexes, and triggers from the specification.
2. Store secrets in AWS Secrets Manager as JSON.
3. Set Lambda environment variables.
4. Deploy using the AWS Lambda .NET tooling.

## Local run
1. Restore and build the project.
2. Set the required environment variables.
3. Invoke the Lambda handler with API Gateway proxy events.

## Notes
- Raw SQL only.
- Parameterized queries only.
- No ORM is used.
- Sensitive fields must not be logged.
- Payment verification is atomic within a PostgreSQL transaction.
