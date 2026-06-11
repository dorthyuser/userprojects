# Life Time Calculator Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite for local development
- Azure subscription for deployment
- Optional: Azure Key Vault access if using secret resolution

## Environment Variables
Set these values in `local.settings.json` or Azure Function App configuration:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
- `AZURE_KEY_VAULT_URI` optional, used for primary secret resolution
- `APPINSIGHTS_CONNECTION_STRING` optional

## Secret Resolution
Secrets are resolved in this order:
1. Azure Key Vault using `AZURE_KEY_VAULT_URI` and `DefaultAzureCredential`
2. Environment variable fallback via `SecretHelper.Get(secretName, envFallback)`

## Local Run Steps
1. Restore packages: `dotnet restore`
2. Build: `dotnet build`
3. Start Azurite if needed
4. Run Functions locally: `func start`

## Deployment Steps
1. Publish: `dotnet publish -c Release`
2. Create or update an Azure Function App using .NET 8 isolated worker
3. Configure application settings in Azure Portal
4. Deploy using `func azure functionapp publish <function-app-name>` or CI/CD

## API Endpoints

### GET /api/lifetime-calculator
- Description: Calculates exact elapsed lifetime from the provided date of birth until the current UTC moment.
- Required Headers: None
- Query Parameters:
  - `dateOfBirth` required, ISO 8601 date-time string
- Path Parameters: None
- Request Body: Optional JSON body accepted if query parameter is omitted

json
{
  "dateOfBirth": "1990-01-01T00:00:00Z"
}


- Example Successful Response:

json
{
  "dateOfBirth": "1990-01-01T00:00:00.0000000Z",
  "currentDate": "2026-06-11T00:00:00.0000000Z",
  "age": {
    "years": 36,
    "months": 5,
    "weeks": 1,
    "days": 3,
    "hours": 12,
    "minutes": 30,
    "seconds": 45
  },
  "lifetimeStats": {
    "totalYearsLived": 36.44,
    "totalMonthsLived": 437.28,
    "totalWeeksLived": 1890.12,
    "totalDaysLived": 13231.52,
    "totalHoursLived": 317556.48,
    "totalMinutesLived": 19053388.8,
    "totalSecondsLived": 1143203328
  }
}


- Example Error Response:

json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "dateOfBirth is required."
  }
}


- Sample CURL:

bash
curl -X GET "http://localhost:7071/api/lifetime-calculator?dateOfBirth=1990-01-01T00:00:00Z"


## Notes
- Only one HTTP endpoint is implemented.
- Responses are JSON only.
- Sensitive values are not logged.
- Errors are returned in a structured format without leaking stack traces or governed data.
