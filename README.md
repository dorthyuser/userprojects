# Life Time Calculator Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite for local development
- Azure subscription for deployment
- Optional: Azure Key Vault access if using secret resolution

## Environment Variables
Set these in `local.settings.json` for local development or in Azure Function App configuration for deployment:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
- `AZURE_KEY_VAULT_URI` optional; if set, secrets are resolved from Key Vault first
- `APPLICATIONINSIGHTS_CONNECTION_STRING` optional

## Secret Resolution
The project uses a two-factor resolution pattern:
1. Azure Key Vault via `SecretHelper.Get(secretName, envFallback)` when `AZURE_KEY_VAULT_URI` is configured.
2. Environment variable fallback when Key Vault is unavailable or the secret cannot be retrieved.

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Build the project:
   - `dotnet build`
3. Run locally:
   - `func start`

## Deployment Steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings in Azure:
   - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
   - `AzureWebJobsStorage`
   - `AZURE_KEY_VAULT_URI` if using Key Vault
3. Publish:
   - `func azure functionapp publish <FUNCTION_APP_NAME>`

## API Endpoints

### GET /api/lifetime-calculator
- Method: GET
- Full Route: `/api/lifetime-calculator`
- Description: Calculates exact elapsed time from the provided date of birth until the current UTC moment.
- Required Headers: None beyond default Azure Functions auth handling.
- Query Parameters:
  - `dateOfBirth` required, ISO 8601 date-time string
- Path Parameters: None
- Request Body: None
- Example Successful Response:
json
{
  "dateOfBirth": "1990-01-01T00:00:00Z",
  "currentDate": "2026-06-08T00:00:00Z",
  "age": {
    "years": 36,
    "months": 5,
    "weeks": 1890,
    "days": 13234,
    "hours": 317616,
    "minutes": 19056960,
    "seconds": 1143417600
  },
  "lifetimeStats": {
    "totalYearsLived": 36.438,
    "totalMonthsLived": 437.256,
    "totalWeeksLived": 1890.571429,
    "totalDaysLived": 13234.0,
    "totalHoursLived": 317616.0,
    "totalMinutesLived": 19056960.0,
    "totalSecondsLived": 1143417600.0
  }
}

- Example Error Response:
json
{
  "error": "Validation failed",
  "details": "Query parameter 'dateOfBirth' is required."
}

- Sample CURL:
bash
curl -X GET "https://localhost:7071/api/lifetime-calculator?dateOfBirth=1990-01-01T00:00:00Z"


## Notes
- The function returns JSON only.
- Error responses are structured and do not expose stack traces or sensitive data.
- Logging is limited to entry, exit, and error events to avoid exposing governed data.
