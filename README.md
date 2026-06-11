# Life Time Calculator Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite for local development
- Azure subscription for deployment
- Optional: Azure Key Vault access if using secret resolution

## Environment Variables
Set these values in `local.settings.json` for local development or in Azure Function App configuration for deployment:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated`
- `AZURE_KEY_VAULT_URI` = Key Vault URI used by `SecretHelper`
- `APPINSIGHTS_INSTRUMENTATIONKEY` = optional telemetry key

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Build the project:
   - `dotnet build`
3. Run locally:
   - `func start`
4. Test the endpoint using the examples below.

## Deployment Steps (Azure Functions)
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings:
   - `AzureWebJobsStorage`
   - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
   - `AZURE_KEY_VAULT_URI` if using Key Vault
3. Publish the app:
   - `func azure functionapp publish <FUNCTION_APP_NAME>`
4. Verify the function route is available at `/api/lifetime-calculator`.

## API Endpoints

### GET /api/lifetime-calculator
- **Description:** Calculates exact elapsed lifetime statistics from `dateOfBirth` until the current UTC moment.
- **Required Headers:** None
- **Query Parameters:**
  - `dateOfBirth` (required): ISO 8601 date-time string
- **Path Parameters:** None
- **Request Body:** None
- **Example Successful Response:**
json
{
  "dateOfBirth": "1990-01-01T00:00:00.0000000Z",
  "currentDate": "2026-06-11T00:00:00.0000000Z",
  "age": {
    "years": 36,
    "months": 5,
    "weeks": 1,
    "days": 3,
    "hours": 0,
    "minutes": 0,
    "seconds": 0
  },
  "lifetimeStats": {
    "totalYearsLived": 36.44,
    "totalMonthsLived": 437.28,
    "totalWeeksLived": 1890.14,
    "totalDaysLived": 13231.00,
    "totalHoursLived": 317544.00,
    "totalMinutesLived": 19052640.00,
    "totalSecondsLived": 1143158400.00
  }
}

- **Example Error Response:**
json
{
  "error": {
    "code": "400",
    "message": "dateOfBirth is required."
  }
}

- **Sample CURL Command:**
bash
curl -X GET "http://localhost:7071/api/lifetime-calculator?dateOfBirth=1990-01-01T00:00:00Z"


## Notes on Security and Logging
- Sensitive values are never returned in responses.
- Error handling returns structured JSON without stack traces.
- Logging is limited to entry, exit, and error events.
- `SecretHelper` resolves secrets from Azure Key Vault first, then environment variables as fallback.
