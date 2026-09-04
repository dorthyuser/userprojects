# Buyandsellgold1013 Lambda

## Overview
.NET 8 AWS Lambda behind API Gateway using ASP.NET Core Lambda hosting model patterns and PostgreSQL via Npgsql.

## Environment Variables
- AWS_SECRET_NAME: Secrets Manager secret name containing JSON keys
- host: PostgreSQL host fallback
- port: PostgreSQL port fallback
- dbname: PostgreSQL database fallback
- username: PostgreSQL username fallback
- password: PostgreSQL password fallback
- LOG_LEVEL: optional logging level

## Secrets Manager Keys
- host
- port
- dbname
- username
- password

## Build and Deploy
1. Restore packages: `dotnet restore`
2. Build: `dotnet build -c Release`
3. Publish: `dotnet publish -c Release -o publish`
4. Deploy with AWS Lambda tooling using `aws-lambda-tools-defaults.json`

## Local Run
1. Set environment variables for database connectivity.
2. Run the Lambda locally with API Gateway event payloads.
3. Ensure PostgreSQL is reachable from your environment.

## Notes
- No secrets are stored in appsettings.json.
- All SQL is parameterized.
- No PostgreSQL enum types are defined in the schema, so no enum mapping is used.
- Logging must not include sensitive personal or payment data.