# create-travelcard-prod

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL 14+
- Azure Storage Emulator or Azurite for local development
- Azure subscription for deployment
- Optional: Azure Key Vault and Managed Identity for secret resolution

## Environment Variables
The application resolves secrets using the mandatory two-factor pattern:
1. Azure Key Vault via `AZURE_KEY_VAULT_URI`
2. Environment variable fallback

Required variables:
- `AZURE_KEY_VAULT_URI` - Key Vault URI, optional but recommended
- `PostgresConnectionString` - PostgreSQL connection string fallback and secret name
- `AzureWebJobsStorage` - required by Azure Functions runtime
- `FUNCTIONS_WORKER_RUNTIME` - must be `dotnet-isolated`

If Key Vault is enabled, the app attempts to resolve:
- Secret name: `PostgresConnectionString`
- Fallback env var: `PostgresConnectionString`

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Start PostgreSQL and ensure the schema exists.
3. Update `local.settings.json` with valid values.
4. Run the function app:
   - `func start`
   - or `dotnet run`

## Deployment Steps (Azure Functions)
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings:
   - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
   - `AzureWebJobsStorage`
   - `AZURE_KEY_VAULT_URI` if using Key Vault
   - `PostgresConnectionString` if not using Key Vault
3. Deploy using one of the following:
   - `func azure functionapp publish <function-app-name>`
   - GitHub Actions / Azure DevOps pipeline
4. Ensure the Function App managed identity has Key Vault secret get permissions if Key Vault is used.

## API Endpoints

### POST /api/travelcard
Creates a new travelcard record.

Required Headers:
- `client_id` - required, 1 to 128 characters, pattern `^[\w+]+$`
- `Content-Type: application/json`
- `X-Correlation-Cust-Id` - optional, up to 100 characters, pattern `^[A-Za-z0-9_-]+$`

Query Parameters:
- None

Path Parameters:
- None

Request Body Example:
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-01T00:00:00Z",
  "travelcardValidTo": "2026-12-31T23:59:59Z",
  "travelcardName": "Sample Travelcard",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2025-12-01T10:00:00Z",
  "travelcardTransactionReference": "12ABCD345678901",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-doe-photo.jpg",
      "cardholderPhotoRRSKey": "123e4567-e89b-12d3-a456-426614174000.jpg",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    }
  ]
}

Example Successful Response:
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": ""
}

Example Error Response:
{
  "error": {
    "code": "400",
    "message": "Missing required header: client_id"
  }
}

Sample CURL:
bash
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "client_id: demo-client" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-01-01T00:00:00Z",
    "travelcardValidTo": "2026-12-31T23:59:59Z",
    "travelcardName": "Sample Travelcard",
    "travelcardNumber": "ABC12345678",
    "travelcardRequestedDate": "2025-12-01T10:00:00Z",
    "travelcardTransactionReference": "12ABCD345678901",
    "travelcardUsableTo": null,
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Doe",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john-doe-photo.jpg",
        "cardholderPhotoRRSKey": "123e4567-e89b-12d3-a456-426614174000.jpg",
        "cardholderPhotoURL": null,
        "cardholderPhotoKey": null
      }
    ]
  }'
