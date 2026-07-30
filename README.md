# csharpae1039

.NET 8 AWS Lambda service for adverse event submission and notification retrieval.

## Endpoints
- `POST /v1/adverse-events`
- `GET /v1/adverse-events/notifications`

## Notes
- Uses raw SQL with PostgreSQL via Npgsql.
- Applies CTCAE coercion rules before persistence.
- Writes AE, notification, and audit rows in a single transaction.
- Secrets are loaded from AWS Secrets Manager with environment fallback.
- Logging avoids sensitive payload values.

## Build

dotnet restore
dotnet build -c Release


## Deploy
Use the provided `aws-lambda-tools-defaults.json` with the handler:
`Csharpae1039Lambda::Csharpae1039Lambda.Function::Csharpae1039`