# Azure Functions Travelcard API

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- Azure Storage Emulator or Azurite for local development
- Access to Azure Key Vault if using secret resolution in Azure

## Environment Variables
The function app reads configuration using Key Vault first, then environment variables as fallback.

### PostgreSQL
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`

### Key Vault
- `AZURE_KEY_VAULT_URI`

### Local runtime
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
- `AzureWebJobsFeatureFlags=EnableWorkerIndexing`

## Local Run Steps
1. Restore packages:
   ```bash
   dotnet restore
   ```
2. Set values in `local.settings.json` or your shell environment.
3. Run locally:
   ```bash
   func start
   ```

## Deployment Steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings for all environment variables listed above.
3. If using Key Vault, set `AZURE_KEY_VAULT_URI` and grant the Function App managed identity access to secrets.
4. Publish:
   ```bash
   func azure functionapp publish <function-app-name>
   ```

## API Endpoints

### 1) Health
- **Method:** GET
- **Full Route:** `/api/health`
- **Description:** Returns a simple health check response.
- **Required Headers:** `client_id` via Function authorization is not applicable; no custom headers are required.
- **Query Parameters:** None
- **Path Parameters:** None
- **Request Body:** None
- **Example Successful Response:**
```json
OK
```
- **Example Error Response:**
```json
{
  "error": "Internal server error",
  "details": "<message>"
}
```
- **Sample CURL:**
```bash
curl -X GET "https://localhost:7071/api/health" -H "x-functions-key: <function_key>"
```

### 2) Create Travelcard
- **Method:** POST
- **Full Route:** `/api/travelcard`
- **Description:** Creates a new travelcard and dependent cardholder records in PostgreSQL.
- **Required Headers:**
  - `client_id` required
  - `Content-Type: application/json`
  - `X-Correlation-Cust-Id` optional
- **Query Parameters:** None
- **Path Parameters:** None
- **Request Body:**
```json
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-05-01T00:00:00Z",
  "travelcardValidTo": "2026-06-01T00:00:00Z",
  "travelcardName": "Summer Card",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-04-20T12:00:00Z",
  "travelcardTransactionReference": "123456789012345",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Smith",
      "cardholderType": "Primary",
      "cardholderPhotoName": "profile-photo",
      "cardholderPhotoURL": "https://example.com/photo.jpg"
    }
  ]
}
```
- **Example Successful Response:**
```json
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}
```
- **Example Error Response:**
```json
{
  "error": "Validation failed",
  "details": "travelcardRequestedDate must be in the past."
}
```
- **Sample CURL:**
```bash
curl -X POST "https://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: sample-client" \
  -H "x-functions-key: <function_key>" \
  -d '{
    "travelcardType":"Young",
    "travelcardValidFrom":"2026-05-01T00:00:00Z",
    "travelcardValidTo":"2026-06-01T00:00:00Z",
    "travelcardName":"Summer Card",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-04-20T12:00:00Z",
    "travelcardTransactionReference":"123456789012345",
    "travelcardUsableTo":null,
    "cardholders":[{"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Smith","cardholderType":"Primary","cardholderPhotoName":"profile-photo","cardholderPhotoURL":"https://example.com/photo.jpg"}]
  }'
```

## Notes
- Enum values must match exactly: `Young`, `Barcklays`, `DevonandCornwall`, `TwoTogether`, `Family`, `Senior`, `DisabledPersons`, `Network`, `TwentySixToThirty`, `SixteenToSeventeen`, `Veterans`.
- Cardholder types must match exactly: `Primary`, `Secondary`.
- For travelcards of type `SixteenToSeventeen`, `travelcardUsableTo` is required.
- Secondary cardholders are not allowed for `SixteenToSeventeen` and `Veterans`.
