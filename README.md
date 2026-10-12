# PrimeNumberService

PrimeNumberService is a production-ready ASP.NET Core 8 migration of the legacy MuleSoft/XML/APIkit project. It exposes a single business endpoint that returns a prime number between 100 and 200, sourced from PostgreSQL and configured through AWS Secrets Manager.

## Features
- GET `/prime` returns a validated prime number in the range 100..200
- Secure secret retrieval from AWS Secrets Manager
- PostgreSQL connection pooling via Npgsql
- Structured logging with sensitive data excluded
- Centralized exception handling with safe error responses
- Health endpoint at `/health`
- No account-details lookup endpoint is included

## API
### GET /prime
Response:

{
  "prime_number": 101
}


## Configuration
Set the following configuration values or environment variables:
- `Service:SecretsManagerSecretName`
- `Service:DefaultRegion`
- `Service:Database:SearchPath`
- `Service:Database:ConnectTimeoutSeconds`
- `Service:Database:CommandTimeoutSeconds`
- `Service:Database:MinPoolSize`
- `Service:Database:MaxPoolSize`

AWS secret JSON shape:

{
  "host": "db.example.internal",
  "port": 5432,
  "dbname": "appdb",
  "username": "appuser",
  "password": "***"
}


## Run locally
bash
dotnet restore
dotnet run --project src/PrimeNumberService


## Security notes
- No PCI, PII, token, fraud, or AML governed data is logged.
- Exceptions are sanitized and do not expose secret values or stack traces to clients.
- Request/response contracts are strict and do not allow extra fields in the prime response model.

## Build
bash
dotnet build PrimeNumberService.sln


## Test
No automated tests are included in the legacy source. Add integration tests against a test PostgreSQL instance as needed.
