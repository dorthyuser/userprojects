# Travelcard Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite for local development
- PostgreSQL database
- Azure subscription for deployment
- Access to Azure Key Vault if using secret resolution

## Environment Variables
Required configuration is resolved using Azure Key Vault first, then environment variables.

### Key Vault / Environment Fallback Names
- `AZURE_KEY_VAULT_URI`
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`

### local.settings.json sample values
- `AzureWebJobsStorage=UseDevelopmentStorage=true`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
- `AZURE_KEY_VAULT_URI=`
- `POSTGRESQLHOST=localhost`
- `POSTGRESQLPORT=5432`
- `POSTGRESQLDATABASE=travelcarddb`
- `POSTGRESQLUSERNAME=postgres`
- `POSTGRESQLPASSWORD=postgres`

## Local Run Steps
1. Restore packages: `dotnet restore`
2. Build: `dotnet build`
3. Run locally: `func start`
4. Send a POST request to `/api/travelcards`

## Deployment Steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings:
   - `AZURE_KEY_VAULT_URI` if using Key Vault
   - `POSTGRESQLHOST`
   - `POSTGRESQLPORT`
   - `POSTGRESQLDATABASE`
   - `POSTGRESQLUSERNAME`
   - `POSTGRESQLPASSWORD`
3. Deploy with `func azure functionapp publish <function-app-name>` or from CI/CD.
4. Verify the Function App has access to PostgreSQL and Key Vault managed identity permissions.

## API Endpoints

### POST /api/travelcards
- **Description:** Creates a new travelcard and dependent cardholder(s).
- **Required Headers:**
  - `client_id` string, required, 1 to 128 characters
  - `Content-Type: application/json`
  - `X-Correlation-Cust-Id` optional, up to 100 characters
- **Query Parameters:** None
- **Path Parameters:** None
- **Request Body:**
json
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-01T10:00:00Z",
  "travelcardValidTo": "2026-12-31T23:59:59Z",
  "travelcardName": "My Travelcard",
  "travelcardNumber": "12345678901",
  "travelcardRequestedDate": "2025-01-01T10:00:00Z",
  "travelcardTransactionReference": "A12345678901234",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Smith",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-photo",
      "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd"
    },
    {
      "cardholderTitle": "Mrs",
      "cardholderForename": "Jane",
      "cardholderSurname": "Smith",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "jane-photo",
      "cardholderPhotoURL": "https://example.com/photo.jpg"
    }
  ]
}

- **Example Successful Response:**
json
{
  "travelcardId": 1,
  "token": "P5SSY6"
}

- **Example Error Response:**
json
{
  "error": "Validation failed",
  "details": "travelcardRequestedDate must be in the past"
}

- **Sample cURL:**
bash
curl -X POST "http://localhost:7071/api/travelcards" \
  -H "client_id: my-client" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-01-01T10:00:00Z",
    "travelcardValidTo": "2026-12-31T23:59:59Z",
    "travelcardName": "My Travelcard",
    "travelcardNumber": "12345678901",
    "travelcardRequestedDate": "2025-01-01T10:00:00Z",
    "travelcardTransactionReference": "A12345678901234",
    "travelcardUsableTo": null,
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Smith",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john-photo",
        "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd"
      }
    ]
  }'
