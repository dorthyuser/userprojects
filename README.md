# Azure Functions Travelcard API

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- Azure subscription if deploying to Azure Functions
- Optional: Azure Key Vault for secret resolution

## Environment Variables
Set these values locally in `local.settings.json` or in Azure App Settings:
- `AZURE_KEY_VAULT_URI` - Key Vault URI for secret retrieval
- `POSTGRESQL_HOST` - PostgreSQL host
- `POSTGRESQL_PORT` - PostgreSQL port
- `POSTGRESQL_DATABASE` - PostgreSQL database name
- `POSTGRESQL_USERNAME` - PostgreSQL username
- `POSTGRESQL_PASSWORD` - PostgreSQL password
- `PostgresConnectionString` - Optional full PostgreSQL connection string fallback
- `AzureWebJobsStorage` - Required by Azure Functions
- `FUNCTIONS_WORKER_RUNTIME` - Must be `dotnet-isolated`

### Secret Resolution Order
1. Azure Key Vault via `AZURE_KEY_VAULT_URI`
2. Environment variable fallback

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Build the project:
   - `dotnet build`
3. Run Azure Functions locally:
   - `func start`

## Deployment Steps to Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings in Azure Portal.
3. If using Key Vault, set `AZURE_KEY_VAULT_URI` and grant Managed Identity access.
4. Deploy using one of the following:
   - Visual Studio publish
   - `func azure functionapp publish <app-name>`
   - GitHub Actions CI/CD

## API Endpoints

### POST /api/travelcard
- Description: Creates a new travelcard and dependent cardholder records in PostgreSQL.
- Required Headers:
  - `client_id` (required, 1-128 characters)
  - `Content-Type: application/json`
  - `X-Correlation-Cust-Id` (optional, up to 100 characters)
- Query Parameters: None
- Path Parameters: None

#### Request Body Example
```json
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-01T10:00:00Z",
  "travelcardValidTo": "2026-12-31T23:59:59Z",
  "travelcardName": "My Travelcard",
  "travelcardNumber": "ABC123456789",
  "travelcardRequestedDate": "2025-12-01T09:00:00Z",
  "travelcardTransactionReference": "A12345678901234",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Smith",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-smith-photo",
      "cardholderPhotoURL": "https://example.com/photos/john-smith.jpg"
    }
  ]
}
```

#### Example Successful Response
```json
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}
```

#### Example Error Response
```json
{
  "error": "Validation failed",
  "details": "travelcardRequestedDate must be in the past."
}
```

#### Sample CURL Command
```bash
curl -X POST "https://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client-id" \
  -H "X-Correlation-Cust-Id: corr-123" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-01-01T10:00:00Z",
    "travelcardValidTo": "2026-12-31T23:59:59Z",
    "travelcardName": "My Travelcard",
    "travelcardNumber": "ABC123456789",
    "travelcardRequestedDate": "2025-12-01T09:00:00Z",
    "travelcardTransactionReference": "A12345678901234",
    "travelcardUsableTo": null,
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Smith",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john-smith-photo",
        "cardholderPhotoURL": "https://example.com/photos/john-smith.jpg"
      }
    ]
  }'
```