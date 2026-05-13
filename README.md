# Travelcard Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- Optional: Azure Key Vault access with Managed Identity or developer credentials

## Environment Variables
Set the following values in `local.settings.json` for local development or in Azure Function App configuration for deployment:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
- `POSTGRESQL_HOST`
- `POSTGRESQL_PORT`
- `POSTGRESQL_DATABASE`
- `POSTGRESQL_USERNAME`
- `POSTGRESQL_PASSWORD`
- `AZURE_KEY_VAULT_URI` optional

If `AZURE_KEY_VAULT_URI` is set, secrets are resolved from Key Vault first using these secret names:
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`

Fallback environment variable names used by the application:
- `POSTGRESQL_HOST`
- `POSTGRESQL_PORT`
- `POSTGRESQL_DATABASE`
- `POSTGRESQL_USERNAME`
- `POSTGRESQL_PASSWORD`

## Local Run Steps
1. Restore packages:
   `dotnet restore`
2. Build the project:
   `dotnet build`
3. Run locally:
   `func start`

## Deployment Steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings with the PostgreSQL values and optional Key Vault URI.
3. Publish the function app:
   `func azure functionapp publish <FUNCTION_APP_NAME>`
4. Confirm the function is reachable at the `/api/travelcards` route.

## API Endpoints

### POST /api/travelcards
**Description:** Creates a new travelcard and one or two dependent cardholders in PostgreSQL.

**Required Headers:**
- `client_id` string, required, 1 to 128 characters
- `Content-Type: application/json`
- `X-Correlation-Cust-Id` string, optional, max 100 characters

**Query Parameters:** None

**Path Parameters:** None

**Request Body Example:**
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-06-01T10:00:00Z",
  "travelcardValidTo": "2026-12-01T10:00:00Z",
  "travelcardName": "Summer Pass",
  "travelcardNumber": "ABC123456789",
  "travelcardRequestedDate": "2026-05-01T10:00:00Z",
  "travelcardTransactionReference": "123456789012345",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Smith",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-smith-photo",
      "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd"
    }
  ]
}

**Example Successful Response:**
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

**Example Error Response:**
{
  "error": "Validation failed",
  "details": "travelcardRequestedDate must be in the past."
}

**Sample CURL Command:**
```bash
curl -X POST "http://localhost:7071/api/travelcards" \
  -H "client_id: my-client" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Cust-Id: corr-123" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-06-01T10:00:00Z",
    "travelcardValidTo": "2026-12-01T10:00:00Z",
    "travelcardName": "Summer Pass",
    "travelcardNumber": "ABC123456789",
    "travelcardRequestedDate": "2026-05-01T10:00:00Z",
    "travelcardTransactionReference": "123456789012345",
    "travelcardUsableTo": null,
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Smith",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john-smith-photo",
        "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd"
      }
    ]
  }'
```
