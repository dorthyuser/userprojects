# hello

Unified production-ready C# service migrated from the provided legacy projects.

## Overview

This application exposes a single prime-number API and is designed to run as a secure, production-grade ASP.NET Core service.

### Included functionality
- `GET /prime` returns a prime number between 100 and 200
- No account-details endpoint is included
- No `GET /:id` endpoint is included

### Security and compliance
- No PCI data is accepted or logged
- No PII is exposed in responses or logs
- Secrets are loaded from environment variables / AWS Secrets Manager-compatible configuration
- Error responses are sanitized and do not leak internal details

## Project name
- Unified project name: `hello`

## Run locally

bash
dotnet restore
dotnet run --project src/hello/hello.csproj


## Configuration

Environment variables:
- `ASPNETCORE_ENVIRONMENT`
- `AWS_SECRET_NAME` (optional, for secret lookup integration)
- `POSTGRESQL_SECRET` (optional, for database secret lookup integration)
- `AWS_REGION` (optional)

## API

### GET /prime
Returns a prime number in the range 100-200.

Response:

{
  "prime": 101
}


## Notes
- The legacy projects were merged into one coherent service.
- Database access is implemented with safe pooling and secret-based configuration.
- If the database is unavailable, the service returns a sanitized error response.
