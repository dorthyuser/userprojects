# combined_project

combined_project is a unified .NET 8 AWS Lambda API that combines the compatible functionality from the legacy source projects into one production-ready service.

## Included functionality
- Travelcard creation API
- Prime number retrieval API
- AWS Lambda deployment support
- Secrets Manager-backed PostgreSQL connectivity
- Structured logging and safe error handling

## Security and compliance
This project is designed with PCI DSS, PII, tokenisation, fraud detection, and AML controls as hard boundaries.

- Sensitive fields are not logged.
- Exceptions do not expose governed data.
- Request validation rejects malformed payloads without leaking internals.
- Database and secret values are loaded from AWS Secrets Manager.
- No account-details GET-by-id endpoint is included.

## API endpoints
- `GET /prime` - returns a prime number between 100 and 200 from the database
- `POST /travelcards` - creates a travelcard record and associated cardholders

## Notes
- The legacy account-details endpoint has been intentionally removed.
- The project name is standardized as `combined_project` across metadata and documentation.
- The application is intended to run as an AWS Lambda behind a Function URL or API Gateway.

## Build
bash
dotnet restore
dotnet build -c Release


## Run locally
bash
dotnet run


## Environment variables
- `AWS_SECRET_NAME` - AWS Secrets Manager secret name containing PostgreSQL credentials
- `AWS_REGION` - AWS region for Secrets Manager and Lambda deployment
- `POSTGRESQL_SECRET` - optional legacy secret name fallback for prime database connectivity
- `POSTGRESQLHOST`, `POSTGRESQLPORT`, `POSTGRESQLDATABASE`, `POSTGRESQLUSERNAME`, `POSTGRESQLPASSWORD` - optional fallbacks

## Deployment
The repository includes GitHub Actions and deployment scripts adapted from the legacy projects.
