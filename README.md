# TravelCardFunctionApp

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite
- PostgreSQL 14+
- Access to Azure Key Vault if using secret resolution in Azure

## Environment Variables
Set the following values locally or in Azure App Settings:
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`
- `POSTGRESQL_CONNECTION_STRING`
- `AZURE_KEY_VAULT_URI`

Secret resolution order:
1. Azure Key Vault secret via `SecretHelper.Get("PostgresConnectionString", "POSTGRESQL_CONNECTION_STRING")`
2. Environment variable fallback

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Start Azurite if needed.
3. Update `local.settings.json` with your PostgreSQL connection details.
4. Run the function app:
   - `func start`

## Deployment Steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings in Azure:
   - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
   - `POSTGRESQLHOST`
   - `POSTGRESQLPORT`
   - `POSTGRESQLDATABASE`
   - `POSTGRESQLUSERNAME`
   - `POSTGRESQLPASSWORD`
   - `POSTGRESQL_CONNECTION_STRING`
   - `AZURE_KEY_VAULT_URI`
3. Deploy using Visual Studio, Azure Functions Core Tools, or GitHub Actions.
4. Ensure the managed identity has access to Key Vault secrets if Key Vault is used.

## API Endpoints

### POST /api/travelcard
Creates a new travelcard and dependent cardholder records.

Required Headers:
- `client_id` string, required, 1-128 characters
- `Content-Type: application/json`
- `X-Correlation-Cust-Id` string, optional, max 100 characters

Query Parameters:
- None

Path Parameters:
- None

Request Body Example:
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-01T00:00:00Z",
  "travelcardValidTo": "2026-12-31T23:59:59Z",
  "travelcardName": "My Travelcard",
  "travelcardNumber": "12345678901",
  "travelcardRequestedDate": "2025-12-01T10:00:00Z",
  "travelcardTransactionReference": "123456789012345",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Smith",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-smith-photo",
      "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    }
  ]
}

Successful Response Example:
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Error Response Example:
{
  "error": {
    "code": 400,
    "message": "travelcardRequestedDate must be in the past."
  }
}

Sample CURL:
bash
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "client_id: sample-client" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Cust-Id: corr-123" \
  -d '{
    "travelcardType":"Young",
    "travelcardValidFrom":"2026-01-01T00:00:00Z",
    "travelcardValidTo":"2026-12-31T23:59:59Z",
    "travelcardName":"My Travelcard",
    "travelcardNumber":"12345678901",
    "travelcardRequestedDate":"2025-12-01T10:00:00Z",
    "travelcardTransactionReference":"123456789012345",
    "travelcardUsableTo":null,
    "cardholders":[
      {
        "cardholderTitle":"Mr",
        "cardholderForename":"John",
        "cardholderSurname":"Smith",
        "cardholderType":"Primary",
        "cardholderPhotoName":"john-smith-photo",
        "cardholderPhotoRRSKey":"12345678-1234-1234-1234-123456789012.abcd",
        "cardholderPhotoURL":null,
        "cardholderPhotoKey":null
      }
    ]
  }'
