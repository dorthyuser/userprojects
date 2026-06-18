# Adverse Event Reporter API

ASP.NET Core Web API for submitting adverse events and retrieving notification records.

## Endpoints
- POST /v1/adverse-events
- GET /v1/adverse-events/notifications

## Configuration
- Application listens on port 8080.
- PostgreSQL credentials are resolved via Azure Key Vault when AZURE_KEY_VAULT_URI is present.
- Fallback resolution uses environment variables:
  - POSTGRESQLHOST
  - POSTGRESQLPORT
  - POSTGRESQLDATABASE
  - POSTGRESQLUSERNAME
  - POSTGRESQLPASSWORD

## Notes
- Notification publishing is a non-breakable stub and always stores sns_published = false.
- All SQL is parameterized and stored in repositories only.