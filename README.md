# Travelcard Azure Functions (.NET 8 Isolated Worker)

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite for local development
- PostgreSQL database reachable from the function app
- Optional: Azure Key Vault access via Managed Identity or developer credentials

## Environment Variable Setup
Set these values in `local.settings.json` for local development or in Azure Function App Application Settings for deployment:
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`
- `AZURE_KEY_VAULT_URI` optional; when set, secrets are resolved from Key Vault first
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

If Key Vault is configured, the application first tries these secret names and then falls back to the matching environment variables:
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Start Azurite or provide a valid `AzureWebJobsStorage` connection string.
3. Update `local.settings.json` with PostgreSQL credentials.
4. Run the function app:
   - `func start`
   - or `dotnet run`

## Deployment Steps to Azure Functions
1. Create an Azure Function App on .NET 8 isolated worker.
2. Configure application settings:
   - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
   - `AzureWebJobsStorage`
   - PostgreSQL environment variables
   - `AZURE_KEY_VAULT_URI` if using Key Vault
3. Deploy using one of the following:
   - `func azure functionapp publish <app-name>`
   - GitHub Actions / Azure DevOps pipeline
4. Ensure Managed Identity has access to Key Vault secrets if Key Vault is enabled.

## API Endpoints

### 1) POST /api/travelcards
Create a new travelcard and dependent cardholder(s).

#### Method
POST

#### Full Route
`/api/travelcards`

#### Description
Validates the travelcard payload, inserts the travelcard and cardholder rows into PostgreSQL, and returns the created travelcard identifier and token.

#### Required Headers
- `client_id` string, required, 1 to 128 characters
- `Content-Type: application/json`
- `X-Correlation-Cust-Id` string, optional, up to 100 characters
- `AuthorizationLevel.Function` key is required by Azure Functions runtime

#### Query Parameters
None

#### Path Parameters
None

#### Request Body Example
json
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-01T09:00:00Z",
  "travelcardValidTo": "2026-12-31T23:59:59Z",
  "travelcardName": "My Travelcard",
  "travelcardNumber": "12345678901",
  "travelcardRequestedDate": "2025-12-31T10:00:00Z",
  "travelcardTransactionReference": "123456789012345",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-doe-photo",
      "cardholderPhotoURL": "https://example.com/photo.jpg"
    }
  ]
}

#### Example Successful Response
json
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

#### Example Error Response
json
{
  "error": "Validation failed",
  "details": "travelcardRequestedDate must be in the past."
}

#### Sample CURL
bash
curl -X POST "https://<function-app-name>.azurewebsites.net/api/travelcards" \
  -H "Content-Type: application/json" \
  -H "client_id: sample-client" \
  -H "X-Correlation-Cust-Id: corr-123" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-01-01T09:00:00Z",
    "travelcardValidTo": "2026-12-31T23:59:59Z",
    "travelcardName": "My Travelcard",
    "travelcardNumber": "12345678901",
    "travelcardRequestedDate": "2025-12-31T10:00:00Z",
    "travelcardTransactionReference": "123456789012345",
    "travelcardUsableTo": null,
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Doe",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john-doe-photo",
        "cardholderPhotoURL": "https://example.com/photo.jpg"
      }
    ]
  }'

## Notes
- Only one HTTP endpoint is implemented.
- Enum values must match the exact values documented in the request body.
- PostgreSQL enum columns are cast explicitly in SQL.
- Secrets are resolved from Azure Key Vault first and then from environment variables.