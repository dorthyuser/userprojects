# csharpae1039

## Environment variables
- `AWS_SECRET_NAME` - Secrets Manager secret name containing `host`, `port`, `dbname`, `username`, `password`
- `host` - fallback database host
- `port` - fallback database port
- `dbname` - fallback database name
- `username` - fallback database username
- `password` - fallback database password
- `LOG_LEVEL` - optional logging level

## Deploy
1. Restore and build the .NET 8 project.
2. Package and deploy the Lambda using the provided handler.
3. Ensure PostgreSQL tables, sequences, indexes, and triggers exist before deployment.
4. Ensure AWS Secrets Manager contains the database credentials or provide environment fallbacks.

## Local run
1. Set the environment variables above.
2. Run the Lambda project with .NET 8.
3. Invoke the handler with API Gateway proxy events.

## Notes
- Raw SQL only.
- Parameterized queries only.
- No ORM is used.
- Sensitive data must not be logged.
- Validation and coercion occur before database writes.
