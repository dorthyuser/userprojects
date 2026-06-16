# Travelcard Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- Azure subscription for deployment
- Optional: Azure Key Vault access if using secret resolution

## Environment Variables
Set the following values locally or in Azure Function App settings:
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`
- `POSTGRESQLCONNECTIONSTRING`
- `AZURE_KEY_VAULT_URI`
- `azure`

Secret resolution order:
1. Azure Key Vault via `AZURE_KEY_VAULT_URI`
2. Environment variable fallback

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Build:
   - `dotnet build`
3. Run locally:
   - `func start`

## Deployment Steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings with the environment variables above.
3. Deploy using one of the following:
   - `func azure functionapp publish <function-app-name>`
   - GitHub Actions / Azure DevOps pipeline
4. Verify the function route prefix is `api`.

## API Endpoints

### POST /api/travelcard
Creates a new travelcard and dependent cardholder records.

Required Headers:
- `client_id` string, required, pattern `^[\\w+]+$`, length 1 to 128
- `Content-Type` must contain `application/json`
- `X-Correlation-Cust-Id` optional, pattern `^[A-Za-z0-9_-]+$`, max length 100

Query Parameters:
- None

Path Parameters:
- None

Request Body Example:
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-01-01T10:00:00Z",
  "travelcardValidTo": "2026-12-31T23:59:59Z",
  "travelcardName": "My Travelcard",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2025-12-01T10:00:00Z",
  "travelcardTransactionReference": "01ABCD123456789",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "photo1",
      "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.abcd",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    }
  ]
}

Example Successful Response:
{
  "travelcardId": "6",
  "token": "P5SSY6"
}

Example Error Response:
{
  "error": "travelcardRequestedDate must be in the past."
}

Sample CURL:
bash
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "client_id: client123" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType":"Young",
    "travelcardValidFrom":"2026-01-01T10:00:00Z",
    "travelcardValidTo":"2026-12-31T23:59:59Z",
    "travelcardName":"My Travelcard",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2025-12-01T10:00:00Z",
    "travelcardTransactionReference":"01ABCD123456789",
    "travelcardUsableTo":null,
    "cardholders":[
      {
        "cardholderTitle":"Mr",
        "cardholderForename":"John",
        "cardholderSurname":"Doe",
        "cardholderType":"Primary",
        "cardholderPhotoName":"photo1",
        "cardholderPhotoRRSKey":"12345678-1234-1234-1234-123456789012.abcd",
        "cardholderPhotoURL":null,
        "cardholderPhotoKey":null
      }
    ]
  }'


## Validation Rules Implemented
- Requested date must be in the past
- Valid from must not be later than valid to
- Valid to must be in the future
- Valid from must not be later than one calendar month from now
- Usable to is required for `SixteenToSeventeen`
- Usable to must be in the future when provided
- Secondary cardholder is not allowed for `SixteenToSeventeen` and `Veterans`
- Exactly one primary cardholder is required
- Each cardholder must provide exactly one photo identifier field

## Logging
- Entry logs are written when the function starts
- Exit logs are written on success
- Validation and unexpected errors are logged with label `ERROR`
- Governed data is not written to logs

## Notes
- PostgreSQL enum values are mapped using Npgsql enum mapping and SQL casts.
- No token validation is performed.
- Sensitive values are resolved through `SecretHelper` with Key Vault-first fallback behavior.
