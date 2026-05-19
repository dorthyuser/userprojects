# Travelcard Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- Azure subscription for deployment
- Optional Azure Key Vault for secrets

## Environment Variables
Set these values in local.settings.json for local development or in Azure Function App configuration for deployment.

### Required PostgreSQL settings
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`

### Optional Key Vault setting
- `AZURE_KEY_VAULT_URI`

## Secret Resolution Order
Secrets are resolved in this order:
1. Azure Key Vault using `AZURE_KEY_VAULT_URI`
2. Environment variable fallback

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Build the project:
   - `dotnet build`
3. Run locally:
   - `func start`

## Deployment Steps to Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings:
   - `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
   - `AzureWebJobsStorage`
   - `POSTGRESQLHOST`
   - `POSTGRESQLPORT`
   - `POSTGRESQLDATABASE`
   - `POSTGRESQLUSERNAME`
   - `POSTGRESQLPASSWORD`
   - `AZURE_KEY_VAULT_URI` if using Key Vault
3. Deploy using Visual Studio, Azure CLI, or `func azure functionapp publish <app-name>`.
4. Verify the endpoint responds successfully.

## API Endpoints

### 1) POST /api/travelcard
Creates a new travelcard and dependent cardholder records.

#### Required Headers
- `client_id`: string, required, 1 to 128 characters
- `Content-Type`: `application/json`
- `X-Correlation-Cust-Id`: optional, up to 100 characters

#### Query Parameters
- None

#### Path Parameters
- None

#### Request Body Example
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-15T10:00:00Z",
  "travelcardValidTo": "2026-12-15T10:00:00Z",
  "travelcardName": "My Travelcard",
  "travelcardNumber": "12345678901",
  "travelcardRequestedDate": "2025-01-10T10:00:00Z",
  "travelcardTransactionReference": "112233445566778",
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

#### Successful Response Example
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

#### Error Response Example
{
  "error": "validation_error",
  "details": "travelcardRequestedDate must be in the past."
}

#### Sample CURL
curl -X POST "https://localhost:7071/api/travelcard" \
  -H "client_id: sample-client" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Cust-Id: corr-123" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-01-15T10:00:00Z",
    "travelcardValidTo": "2026-12-15T10:00:00Z",
    "travelcardName": "My Travelcard",
    "travelcardNumber": "12345678901",
    "travelcardRequestedDate": "2025-01-10T10:00:00Z",
    "travelcardTransactionReference": "112233445566778",
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
  }'

## Validation Rules
- `travelcardRequestedDate` must be in the past
- `travelcardValidFrom` must not be later than `travelcardValidTo`
- `travelcardValidTo` must be in the future
- `travelcardValidFrom` must not be later than one calendar month from today
- `travelcardUsableTo` is required for `SixteenToSeventeen`
- `travelcardUsableTo` must be in the future when provided
- Secondary cardholder is not allowed for `SixteenToSeventeen` and `Veterans`
- Cardholders must contain exactly one Primary and optionally one Secondary
- Each cardholder must provide exactly one of:
  - `cardholderPhotoRRSKey`
  - `cardholderPhotoURL`
  - `cardholderPhotoKey`

## Notes
- Secrets are never hardcoded.
- Sensitive values are resolved via `SecretHelper`.
- Errors return a structured JSON response.
