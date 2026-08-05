# csharpae1140

.NET 8 AWS Lambda service for adverse event submission and notification retrieval.

## Environment variables
- `AWS_SECRET_NAME` - Secrets Manager secret name containing database credentials
- `host` - PostgreSQL host fallback
- `port` - PostgreSQL port fallback
- `dbname` - PostgreSQL database name fallback
- `username` - PostgreSQL username fallback
- `password` - PostgreSQL password fallback
- `LOG_LEVEL` - optional, defaults to `INFO`

## Deployment
1. Ensure PostgreSQL schema, sequences, indexes, and triggers exist.
2. Configure AWS Secrets Manager secret with keys:
   - `host`
   - `port`
   - `dbname`
   - `username`
   - `password`
3. Deploy the Lambda using the provided handler:
   `Csharpae1140Lambda::Csharpae1140Lambda.Function::Csharpae1140`

## Local run
1. Set the environment variables above.
2. Restore and build the project.
3. Invoke the Lambda with API Gateway proxy events.

## Notes
- Raw SQL only.
- Parameterised queries only.
- No secrets are logged.
- Validation occurs before any database write.
- All database writes for POST occur in a single transaction.