# paymentcsharp441

## Overview
.NET 8 AWS Lambda service for payment initiation, verification, and retrieval backed by PostgreSQL.

## Environment Variables
- `AWS_SECRET_NAME` - Secrets Manager secret name containing DB and gateway credentials
- `host` - PostgreSQL host fallback
- `port` - PostgreSQL port fallback
- `dbname` - PostgreSQL database name fallback
- `username` - PostgreSQL username fallback
- `password` - PostgreSQL password fallback
- `gateway_secret` - Gateway HMAC secret fallback
- `LOG_LEVEL` - Optional log level
- `IDEMPOTENCY_WINDOW_SEC` - Optional duplicate window, default 120
- `GATEWAY_RETRY_COUNT` - Optional gateway retry count, default 2
- `DEFAULT_CURRENCY` - Optional default currency, default INR
- `TAX_RATE_PERCENT` - Optional GST rate, default 18

## Deployment
1. Provision PostgreSQL and create the schema, sequences, indexes, and triggers.
2. Store secrets in AWS Secrets Manager as JSON.
3. Set `AWS_SECRET_NAME` in the Lambda environment.
4. Deploy the Lambda using the provided handler.

## Local Run
1. Set environment variables for database connectivity and secrets.
2. Restore and build the project.
3. Invoke the Lambda with API Gateway proxy events for:
   - `POST /v1/payments`
   - `POST /v1/payments/verify`
   - `GET /v1/payments`

## Security Notes
- No card data is accepted or logged.
- Sensitive gateway and database values are never written to logs.
- SQL uses parameterized commands only.
- Verification uses HMAC-SHA256 before any transactional write.
