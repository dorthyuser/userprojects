# csharpae1140

.NET 8 AWS Lambda service for adverse event submission and notification retrieval.

## Environment variables
- `AWS_SECRET_NAME` - optional AWS Secrets Manager secret name containing database credentials
- `host` - PostgreSQL host fallback
- `port` - PostgreSQL port fallback
- `dbname` - PostgreSQL database name fallback
- `username` - PostgreSQL username fallback
- `password` - PostgreSQL password fallback
- `LOG_LEVEL` - optional logging level

## Deployment
1. Restore and build the project.
2. Package and deploy as a .NET 8 Lambda.
3. Ensure PostgreSQL schema, sequences, and reference tables exist.
4. Configure the Lambda execution role to read AWS Secrets Manager if used.

## Local run
1. Set the environment variables above.
2. Run the project with a local Lambda test harness or invoke the handler directly.
3. Send requests to:
   - `POST /v1/adverse-events`
   - `GET /v1/adverse-events/notifications`

## Notes
- Raw SQL only.
- Parameterised queries only.
- No ORM is used.
- Validation occurs before any database write.
- Sensitive values are not logged.