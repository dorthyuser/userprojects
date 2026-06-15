# Travelcard Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- Azure subscription for deployment
- Optional: Azure Key Vault access via Managed Identity or developer credentials

## Environment Variable Setup
Set the following variables locally or in Azure Function App settings:
- `AZURE_KEY_VAULT_URI` - Key Vault URI used first for secret resolution
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`
- `POSTGRESQL_CONNECTION_STRING` - fallback connection string used if Key Vault secret `PostgresConnectionString` is unavailable

Secret resolution order:
1. `SecretHelper.Get("PostgresConnectionString", "POSTGRESQL_CONNECTION_STRING")`
2. Azure Key Vault secret named `PostgresConnectionString`
3. Environment variable `POSTGRESQL_CONNECTION_STRING`

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Build the project:
   - `dotnet build`
3. Run locally:
   - `func start`

## Deployment Steps (Azure Functions)
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings with the environment variables listed above.
3. If using Key Vault, assign Managed Identity to the Function App and grant secret read access.
4. Publish:
   - `func azure functionapp publish <function-app-name>`

## API Endpoints

### POST /api/travelcard
Creates a new travelcard and dependent cardholder records.

#### Required Headers
- `client_id`: string, required, pattern `^[\w+]+$`, length 1-128
- `Content-Type`: must contain `application/json`
- `X-Correlation-Cust-Id`: optional, pattern `^[A-Za-z0-9_-]+$`, max length 100

#### Query Parameters
- None

#### Path Parameters
- None

#### Request Body Example
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-01T10:00:00Z",
  "travelcardValidTo": "2026-12-31T23:59:59Z",
  "travelcardName": "My Travelcard",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2025-12-01T09:00:00Z",
  "travelcardTransactionReference": "12ABCD345678901",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-photo",
      "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    }
  ]
}

#### Example Successful Response
{
  "travelcardId": "6",
  "token": "P5SSY6"
}

#### Example Error Response
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid or missing client_id header"
  }
}

#### Sample CURL
bash
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "client_id: client123" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-01-01T10:00:00Z",
    "travelcardValidTo": "2026-12-31T23:59:59Z",
    "travelcardName": "My Travelcard",
    "travelcardNumber": "ABC12345678",
    "travelcardRequestedDate": "2025-12-01T09:00:00Z",
    "travelcardTransactionReference": "12ABCD345678901",
    "travelcardUsableTo": null,
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Doe",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john-photo",
        "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd",
        "cardholderPhotoURL": null,
        "cardholderPhotoKey": null
      }
    ]
  }'


#### Validation Notes
- `travelcardRequestedDate` must be in the past
- `travelcardValidFrom` must not be later than one calendar month from now
- `travelcardValidFrom` must not be later than `travelcardValidTo`
- `travelcardValidTo` must be in the future
- `travelcardUsableTo` is required only for `SixteenToSeventeen`
- `travelcardUsableTo` must be in the future when provided
- Secondary cardholder is not allowed for `SixteenToSeventeen` and `Veterans`

#### Database and Security Notes
- PostgreSQL enum values are cast explicitly in SQL
- Sensitive values are not logged
- Validation and runtime errors return structured JSON without stack traces
