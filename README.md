# csharpae1140

## Overview
AWS Lambda (.NET 8) service for adverse event submission and notification retrieval using PostgreSQL.

## Environment Variables
- `AWS_SECRET_NAME` - Secrets Manager secret name containing database credentials
- `host` - PostgreSQL host fallback
- `port` - PostgreSQL port fallback
- `dbname` - PostgreSQL database name fallback
- `username` - PostgreSQL username fallback
- `password` - PostgreSQL password fallback
- `LOG_LEVEL` - Optional logging level

## Deployment
1. Ensure PostgreSQL schema, sequences, indexes, and triggers are created.
2. Configure Lambda environment variables.
3. Store DB credentials in AWS Secrets Manager as JSON keys:
   - `host`
   - `port`
   - `dbname`
   - `username`
   - `password`
4. Deploy the Lambda using the provided handler:
   - `Csharpae1140Lambda::Csharpae1140Lambda.Function::Csharpae1140`

## Local Run
1. Set environment variables for database connectivity.
2. Run the project with .NET 8.
3. Invoke the handler with API Gateway-compatible events.

## Notes
- Raw SQL only.
- Parameterised queries only.
- No ORM.
- Validation occurs before database writes.
- Sensitive values must not be logged.