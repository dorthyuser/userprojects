# csharpae1140

.NET 8 AWS Lambda service for adverse event submission and notification retrieval.

## Environment variables
- `host`
- `port`
- `dbname`
- `username`
- `password`
- `AWS_SECRET_NAME` (optional)
- `LOG_LEVEL` (optional)

## Deployment
1. Build the project for `net8.0`.
2. Package and deploy as an AWS Lambda function.
3. Ensure PostgreSQL schema, sequences, and reference tables exist.
4. Configure environment variables in the deployment platform.

## Local run
1. Set the environment variables above.
2. Run the project with the .NET 8 SDK.
3. Invoke the Lambda handler using API Gateway-compatible events.

## Notes
- Raw SQL only.
- Parameterised queries only.
- No ORM is used.
- Validation occurs before database writes.
- Sensitive data should not be logged.